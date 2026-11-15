namespace SauceDemo.Tests.TestData;

/// <summary>
/// Violações WCAG que o Sauce Demo já tem hoje, por página (ids de regra do axe-core).
/// O gate só falha com violações que não estejam aqui; estas ficam documentadas, e o teste estrito
/// (categoria KnownBug) falha enquanto existirem. Quando uma deixar de aparecer, tira-a daqui.
/// </summary>
public static class KnownAccessibilityViolations
{
    private static readonly Dictionary<string, string[]> ByPage = new()
    {
    };

    public static IReadOnlyCollection<string> For(string pageName) =>
        ByPage.TryGetValue(pageName, out var rules) ? rules : [];
}
