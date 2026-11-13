using System.Reflection;

namespace SauceDemo.Tests.Fixtures;

/// <summary>
/// Compara um screenshot com a baseline versionada em <c>VisualBaselines/{browser}-{so}/{nome}.png</c>.
/// Só o utilizador de referência cria ou atualiza baselines; os outros só comparam.
/// Para regenerar: <c>UPDATE_VISUAL_BASELINES=1</c>.
/// </summary>
public static class VisualBaseline
{
    /// <summary>Fração máxima de pixels diferentes tolerada (0,1%).</summary>
    public const double MaxDifferentRatio = 0.001;

    private static readonly string ProjectDirectory =
        typeof(VisualBaseline).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "ProjectDirectory").Value!;

    private static bool UpdateRequested =>
        Environment.GetEnvironmentVariable("UPDATE_VISUAL_BASELINES") is "1" or "true";

    public static async Task AssertMatchesAsync(IPage page, string name, bool isReference)
    {
        await WaitForStableRenderingAsync(page);
        var actual = await page.ScreenshotAsync(new()
        {
            FullPage = true,
            Animations = ScreenshotAnimations.Disabled,
            Caret = ScreenshotCaret.Hide,
        });

        var baselinePath = BaselinePath(page, name);
        if (!File.Exists(baselinePath) || (UpdateRequested && isReference))
        {
            if (!isReference)
            {
                Assert.Inconclusive($"Sem baseline '{name}'. Corre primeiro o teste com o utilizador de referência.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(baselinePath)!);
            await File.WriteAllBytesAsync(baselinePath, actual);
            TestContext.AddTestAttachment(baselinePath, "Baseline nova");
            Assert.Inconclusive($"Baseline criada em {baselinePath}. Revê a imagem e faz commit.");
        }

        var expected = await File.ReadAllBytesAsync(baselinePath);
        var comparison = await ImageComparer.CompareAsync(page.Context, expected, actual);
        if (!comparison.SizeMismatch && comparison.DifferentRatio <= MaxDifferentRatio)
        {
            return;
        }

        var outputDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "visual-diffs");
        Directory.CreateDirectory(outputDirectory);
        var prefix = Path.Combine(outputDirectory, $"{name}-{ToSafeFileName(TestContext.CurrentContext.Test.Name)}");
        await File.WriteAllBytesAsync($"{prefix}-actual.png", actual);
        TestContext.AddTestAttachment($"{prefix}-actual.png", "Screenshot atual");
        if (comparison.DiffImage is not null)
        {
            await File.WriteAllBytesAsync($"{prefix}-diff.png", comparison.DiffImage);
            TestContext.AddTestAttachment($"{prefix}-diff.png", "Diferenças a vermelho");
        }

        Assert.Fail(comparison.SizeMismatch
            ? $"'{name}': tamanho diferente da baseline (esperado {comparison.ExpectedSize}, atual {comparison.ActualSize})."
            : $"'{name}': {comparison.DifferentPixels} pixels diferentes ({comparison.DifferentRatio:P2}), " +
              $"acima do limite de {MaxDifferentRatio:P2}. Diff em {prefix}-diff.png");
    }

    /// <summary>Espera pelas fontes e por todas as imagens, para o screenshot não apanhar a página a meio.</summary>
    private static Task WaitForStableRenderingAsync(IPage page) =>
        page.EvaluateAsync("""
            async () => {
              await document.fonts.ready;
              await Promise.all(Array.from(document.images).map(image => image.complete
                ? Promise.resolve()
                : new Promise(resolve => { image.onload = image.onerror = resolve; })));
            }
            """);

    private static string BaselinePath(IPage page, string name)
    {
        var browser = page.Context.Browser?.BrowserType.Name ?? "unknown";
        var os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "mac" : "linux";
        return Path.Combine(ProjectDirectory, "VisualBaselines", $"{browser}-{os}", $"{name}.png");
    }

    private static string ToSafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(c => invalid.Contains(c) || c is '"' or '(' or ')' ? '_' : c).ToArray());
    }
}
