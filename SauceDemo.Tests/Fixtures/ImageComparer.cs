using System.Text.Json;

namespace SauceDemo.Tests.Fixtures;

public sealed record ImageComparison(
    long DifferentPixels,
    long TotalPixels,
    string ExpectedSize,
    string ActualSize,
    byte[]? DiffImage)
{
    public bool SizeMismatch => ExpectedSize != ActualSize;

    public double DifferentRatio => TotalPixels == 0 ? 1 : (double)DifferentPixels / TotalPixels;
}

/// <summary>
/// Compara dois PNG pixel a pixel. O Playwright .NET não tem <c>ToHaveScreenshotAsync</c> (só o test runner de Node),
/// e em vez de acrescentar uma biblioteca de imagem usamos o próprio Chromium: descodifica as duas imagens num canvas,
/// conta os pixels diferentes e desenha um diff (diferenças a vermelho sobre a imagem esperada esbatida).
/// </summary>
public static class ImageComparer
{
    private const string CompareScript = """
        async ([expected, actual, threshold]) => {
          const load = base64 => new Promise((resolve, reject) => {
            const image = new Image();
            image.onload = () => resolve(image);
            image.onerror = () => reject(new Error('PNG inválido'));
            image.src = 'data:image/png;base64,' + base64;
          });
          const [a, b] = await Promise.all([load(expected), load(actual)]);
          const expectedSize = `${a.width}x${a.height}`;
          const actualSize = `${b.width}x${b.height}`;
          if (expectedSize !== actualSize) {
            return { different: 0, total: 0, expectedSize, actualSize, diff: null };
          }

          const { width, height } = a;
          const pixelsOf = image => {
            const canvas = new OffscreenCanvas(width, height);
            const context = canvas.getContext('2d');
            context.drawImage(image, 0, 0);
            return context.getImageData(0, 0, width, height).data;
          };
          const pa = pixelsOf(a);
          const pb = pixelsOf(b);

          const output = new OffscreenCanvas(width, height);
          const outputContext = output.getContext('2d');
          const diff = outputContext.createImageData(width, height);
          let different = 0;
          for (let i = 0; i < pa.length; i += 4) {
            const delta = Math.max(Math.abs(pa[i] - pb[i]), Math.abs(pa[i + 1] - pb[i + 1]), Math.abs(pa[i + 2] - pb[i + 2]));
            if (delta > threshold) {
              different++;
              diff.data.set([255, 0, 0, 255], i);
            } else {
              const grey = (pa[i] + pa[i + 1] + pa[i + 2]) / 3;
              diff.data.set([grey, grey, grey, 64], i);
            }
          }
          outputContext.putImageData(diff, 0, 0);

          const blob = await output.convertToBlob({ type: 'image/png' });
          const bytes = new Uint8Array(await blob.arrayBuffer());
          let binary = '';
          for (let i = 0; i < bytes.length; i += 0x8000) {
            binary += String.fromCharCode(...bytes.subarray(i, i + 0x8000));
          }
          return { different, total: width * height, expectedSize, actualSize, diff: btoa(binary) };
        }
        """;

    /// <param name="channelThreshold">Diferença máxima por canal (0-255) para dois pixels contarem como iguais.</param>
    public static async Task<ImageComparison> CompareAsync(
        IBrowserContext context, byte[] expected, byte[] actual, int channelThreshold = 10)
    {
        var page = await context.NewPageAsync();
        try
        {
            var result = await page.EvaluateAsync<JsonElement>(
                CompareScript,
                new object[] { Convert.ToBase64String(expected), Convert.ToBase64String(actual), channelThreshold });

            var diff = result.GetProperty("diff");
            return new ImageComparison(
                result.GetProperty("different").GetInt64(),
                result.GetProperty("total").GetInt64(),
                result.GetProperty("expectedSize").GetString()!,
                result.GetProperty("actualSize").GetString()!,
                diff.ValueKind == JsonValueKind.String ? Convert.FromBase64String(diff.GetString()!) : null);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
