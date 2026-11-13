using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

[TestFixture]
[Category("Inventory")]
public sealed class InventoryTests : AuthenticatedTest
{
    private InventoryPage _inventory = null!;

    [SetUp]
    public async Task OpenInventoryAsync()
    {
        _inventory = await new InventoryPage(Page).GotoAsync();
        await Expect(_inventory.Items).ToHaveCountAsync(Products.TotalInCatalog);
    }

    [Test]
    [Category("Smoke")]
    public async Task Inventory_WhenLoaded_ShowsFullCatalog()
    {
        await Expect(_inventory.Title).ToHaveTextAsync("Products");
        await Expect(_inventory.ItemNames).ToHaveCountAsync(Products.TotalInCatalog);
    }

    [Test]
    public async Task Sort_ByPriceLowToHigh_OrdersPricesAscending()
    {
        await _inventory.SortByAsync(SortOption.PriceLowToHigh);

        var prices = await _inventory.GetPricesAsync();
        Assert.That(prices, Is.Ordered.Ascending);
    }

    [Test]
    public async Task Sort_ByPriceHighToLow_OrdersPricesDescending()
    {
        await _inventory.SortByAsync(SortOption.PriceHighToLow);

        var prices = await _inventory.GetPricesAsync();
        Assert.That(prices, Is.Ordered.Descending);
    }

    [Test]
    public async Task Sort_ByNameZToA_OrdersNamesDescending()
    {
        await _inventory.SortByAsync(SortOption.NameDescending);

        var names = await _inventory.GetNamesAsync();
        Assert.That(names, Is.Ordered.Descending.Using((IComparer<string>)StringComparer.Ordinal));
    }

    [Test]
    public async Task AddToCart_SingleProduct_UpdatesCartBadge()
    {
        await _inventory.AddToCartAsync(Products.Backpack);

        await Expect(_inventory.Header.CartBadge).ToHaveTextAsync("1");
    }

    [Test]
    public async Task RemoveFromCart_AfterAdding_HidesCartBadge()
    {
        await _inventory.AddToCartAsync(Products.Backpack);
        await _inventory.RemoveFromCartAsync(Products.Backpack);

        await Expect(_inventory.Header.CartBadge).ToBeHiddenAsync();
    }
}
