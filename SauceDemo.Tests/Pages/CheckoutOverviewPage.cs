namespace SauceDemo.Tests.Pages;

public sealed class CheckoutOverviewPage(IPage page) : BasePage(page)
{
    private ILocator FinishButton => Page.ByTestId("finish");

    public ILocator Items => Page.ByTestId("inventory-item");
    public ILocator SubtotalLabel => Page.ByTestId("subtotal-label");

    public async Task<CheckoutCompletePage> FinishAsync()
    {
        await FinishButton.ClickAsync();
        return new CheckoutCompletePage(Page);
    }
}
