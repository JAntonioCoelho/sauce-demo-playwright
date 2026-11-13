namespace SauceDemo.Tests.Pages;

public sealed class CartPage(IPage page) : BasePage(page)
{
    private ILocator CheckoutButton => Page.ByTestId("checkout");

    public ILocator Items => Page.ByTestId("inventory-item");

    public Task RemoveAsync(string productName) =>
        Items.Filter(new() { HasText = productName })
            .GetByRole(AriaRole.Button, new() { Name = "Remove" })
            .ClickAsync();

    public async Task<CheckoutInformationPage> ProceedToCheckoutAsync()
    {
        await CheckoutButton.ClickAsync();
        return new CheckoutInformationPage(Page);
    }
}
