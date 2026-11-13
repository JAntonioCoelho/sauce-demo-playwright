using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

/// <summary>
/// Cada teste descreve o comportamento correto e corre com dois utilizadores:
/// o standard_user prova que o teste está certo; o problem_user tem bugs conhecidos
/// e o teste tem de os apanhar.
/// </summary>
[TestFixture]
[Category("UserBehaviour")]
public sealed class ProblemUserTests : BaseTest
{
    private static IEnumerable<TestCaseData> UsersUnderTest()
    {
        yield return new TestCaseData(Users.Standard);
        yield return new TestCaseData(Users.Problem).SetCategory(TestCategories.KnownBug);
    }

    [TestCaseSource(nameof(UsersUnderTest))]
    public async Task Inventory_EachProduct_ShowsItsOwnImage(string user)
    {
        var inventory = await LoginAsAsync(user);

        var sources = await inventory.GetImageSourcesAsync();

        Assert.That(sources, Is.Unique, "Cada produto devia mostrar a sua própria imagem.");
    }

    [TestCaseSource(nameof(UsersUnderTest))]
    public async Task Sort_ByPriceLowToHigh_OrdersPricesAscending(string user)
    {
        var inventory = await LoginAsAsync(user);

        await inventory.SortByAsync(Pages.SortOption.PriceLowToHigh);

        Assert.That(await inventory.GetPricesAsync(), Is.Ordered.Ascending);
    }

    [TestCaseSource(nameof(UsersUnderTest))]
    public async Task AddToCart_EveryProduct_CountsAllInCartBadge(string user)
    {
        var inventory = await LoginAsAsync(user);

        foreach (var product in await inventory.GetNamesAsync())
        {
            await inventory.AddToCartAsync(product);
        }

        await Expect(inventory.Header.CartBadge).ToHaveTextAsync(Products.TotalInCatalog.ToString());
    }

    [TestCaseSource(nameof(UsersUnderTest))]
    public async Task Checkout_WithValidCustomerInfo_ReachesOverview(string user)
    {
        var inventory = await LoginAsAsync(user);
        await inventory.AddToCartAsync(Products.Backpack);
        var cart = await inventory.Header.OpenCartAsync();
        var information = await cart.ProceedToCheckoutAsync();

        var overview = await information.ContinueWithAsync(CustomerInfo.Valid());

        await Expect(information.ErrorMessage).ToBeHiddenAsync();
        await Expect(overview.Title).ToHaveTextAsync("Checkout: Overview");
    }
}
