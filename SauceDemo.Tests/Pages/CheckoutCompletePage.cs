namespace SauceDemo.Tests.Pages;

public sealed class CheckoutCompletePage(IPage page) : BasePage(page)
{
    public ILocator CompleteHeader => Page.ByTestId("complete-header");
}
