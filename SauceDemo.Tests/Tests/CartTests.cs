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

    /// <summary>Valida o próprio atalho (e os ids em <see cref="Products.Ids"/>): o carrinho preparado tem de aparecer na UI.</summary>
    [Test]
    public async Task Cart_SeededThroughState_ShowsSeededProducts()
    {
        await CartState.SeedAsync(Page, Products.Backpack, Products.Onesie);

        var cart = await new CartPage(Page).GotoAsync();

        await Expect(cart.Items).ToHaveCountAsync(2);
        await Expect(cart.Items.Filter(new() { HasText = Products.Backpack })).ToBeVisibleAsync();
        await Expect(cart.Items.Filter(new() { HasText = Products.Onesie })).ToBeVisibleAsync();
        await Expect(cart.Header.CartBadge).ToHaveTextAsync("2");
    }

    [Test]
    public async Task Cart_RemoveItem_UpdatesListAndBadge()
    {
        await CartState.SeedAsync(Page, Products.Backpack, Products.BikeLight);
        var cart = await new CartPage(Page).GotoAsync();

        await cart.RemoveAsync(Products.Backpack);

        await Expect(cart.Items).ToHaveCountAsync(1);
        await Expect(cart.Header.CartBadge).ToHaveTextAsync("1");
    }
}
