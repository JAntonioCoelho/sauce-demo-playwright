namespace SauceDemo.Tests.Pages;

/// <summary>
/// Base dos Page Objects. As páginas expõem ações e locators;
/// as asserções ficam sempre nos testes.
/// </summary>
public abstract class BasePage(IPage page)
{
    protected IPage Page { get; } = page;

    public ILocator Title => Page.ByTestId("title");
}
