using SauceDemo.Tests.Pages.Components;

namespace SauceDemo.Tests.Pages;

public sealed class CartPage(IPage page) : BasePage(page)
{
    public HeaderComponent Header { get; } = new(page);

    public async Task<CartPage> GotoAsync()
    {
        await Page.GotoAsync("/cart.html");
        return this;
    }

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
