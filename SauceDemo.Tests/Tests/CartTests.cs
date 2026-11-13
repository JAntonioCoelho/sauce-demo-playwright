using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

[TestFixture]
[Category("Cart")]
public sealed class CartTests : AuthenticatedTest
{
    [Test]
    public async Task Cart_WithTwoProducts_ListsBothItems()
    {
        var inventory = await new InventoryPage(Page).GotoAsync();
        await inventory.AddToCartAsync(Products.Backpack);
        await inventory.AddToCartAsync(Products.BikeLight);

        var cart = await inventory.Header.OpenCartAsync();

        await Expect(cart.Title).ToHaveTextAsync("Your Cart");
        await Expect(cart.Items).ToHaveCountAsync(2);
        await Expect(cart.Items.Filter(new() { HasText = Products.Backpack })).ToBeVisibleAsync();
        await Expect(cart.Items.Filter(new() { HasText = Products.BikeLight })).ToBeVisibleAsync();
    }

    [Test]
    public async Task Cart_RemoveItem_UpdatesListAndBadge()
    {
        var inventory = await new InventoryPage(Page).GotoAsync();
        await inventory.AddToCartAsync(Products.Backpack);
        await inventory.AddToCartAsync(Products.BikeLight);
        var cart = await inventory.Header.OpenCartAsync();

        await cart.RemoveAsync(Products.Backpack);

        await Expect(cart.Items).ToHaveCountAsync(1);
        await Expect(inventory.Header.CartBadge).ToHaveTextAsync("1");
    }
}
