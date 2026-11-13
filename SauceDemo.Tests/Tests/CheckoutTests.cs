using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

[TestFixture]
[Category("Checkout")]
public sealed class CheckoutTests : AuthenticatedTest
{
    [Test]
    [Category("Smoke")]
    [Category("E2E")]
    public async Task Checkout_WithValidCustomerInfo_CompletesOrder()
    {
        var inventory = await new InventoryPage(Page).GotoAsync();
        await inventory.AddToCartAsync(Products.Backpack);
        await inventory.AddToCartAsync(Products.Onesie);

        var cart = await inventory.Header.OpenCartAsync();
        var information = await cart.ProceedToCheckoutAsync();
        var overview = await information.ContinueWithAsync(CustomerInfo.Valid());

        await Expect(overview.Title).ToHaveTextAsync("Checkout: Overview");
        await Expect(overview.Items).ToHaveCountAsync(2);

        var complete = await overview.FinishAsync();

        await Expect(complete.Title).ToHaveTextAsync("Checkout: Complete!");
        await Expect(complete.CompleteHeader).ToHaveTextAsync("Thank you for your order!");
        await Expect(inventory.Header.CartBadge).ToBeHiddenAsync();
    }

    [Test]
    public async Task Checkout_WithoutPostalCode_ShowsValidationError()
    {
        var inventory = await new InventoryPage(Page).GotoAsync();
        await inventory.AddToCartAsync(Products.Backpack);
        var cart = await inventory.Header.OpenCartAsync();
        var information = await cart.ProceedToCheckoutAsync();

        await information.FillAsync(CustomerInfo.Valid().WithoutPostalCode());
        await information.SubmitAsync();

        await Expect(information.ErrorMessage).ToContainTextAsync("Postal Code is required");
    }
}
