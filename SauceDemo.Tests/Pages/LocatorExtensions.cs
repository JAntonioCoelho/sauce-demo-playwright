namespace SauceDemo.Tests.Pages;

/// <summary>
/// O Sauce Demo usa o atributo <c>data-test</c> (e não <c>data-testid</c>).
/// Não usamos <c>Selectors.SetTestIdAttribute</c>: no Playwright .NET esse valor é estado global,
/// partilhado entre workers, e com testes em paralelo voltava ao valor por omissão a meio de um teste.
/// Um seletor de atributo explícito não depende de estado nenhum.
/// </summary>
public static class LocatorExtensions
{
    public static ILocator ByTestId(this IPage page, string testId) => page.Locator(Selector(testId));

    public static ILocator ByTestId(this ILocator locator, string testId) => locator.Locator(Selector(testId));

    private static string Selector(string testId) => $"[data-test=\"{testId}\"]";
}
