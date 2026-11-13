using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

/// <summary>
/// O error_user tem bugs que se manifestam como erros: alertas JavaScript e ações que não fazem nada.
/// Cada teste descreve o comportamento correto e corre com o standard_user (tem de passar)
/// e com o error_user (KnownBug: tem de falhar).
/// </summary>
[TestFixture]
[Category("UserBehaviour")]
public sealed class ErrorUserTests : BaseTest
{
    private static IEnumerable<TestCaseData> UsersUnderTest()
    {
        yield return new TestCaseData(Users.Standard);
        yield return new TestCaseData(Users.Error).SetCategory(TestCategories.KnownBug);
    }

    [TestCaseSource(nameof(UsersUnderTest))]
    public async Task Sort_ByPriceLowToHigh_SortsWithoutErrorDialog(string user)
    {
        var inventory = await LoginAsAsync(user);
        using var dialogs = new DialogRecorder(Page);

        await inventory.SelectSortOptionAsync(SortOption.PriceLowToHigh);
        var prices = await inventory.GetPricesAsync();

        Assert.Multiple(() =>
        {
            Assert.That(dialogs.Messages, Is.Empty, "A ordenação não devia abrir diálogos de erro.");
            Assert.That(prices, Is.Ordered.Ascending, "Os preços deviam estar por ordem crescente.");
        });
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
    public async Task RemoveFromCart_EveryProductInCart_EmptiesCart(string user)
    {
        var inventory = await LoginAsAsync(user);
        foreach (var product in await inventory.GetNamesAsync())
        {
            await inventory.AddToCartAsync(product);
        }

        // Remove só o que entrou de facto, para este teste não depender do de cima.
        var inCart = await inventory.GetNamesInCartAsync();
        Assume.That(inCart, Is.Not.Empty, "Nenhum produto entrou no carrinho, não há nada para remover.");
        foreach (var product in inCart)
        {
            await inventory.RemoveFromCartAsync(product);
        }

        await Expect(inventory.Header.CartBadge).ToBeHiddenAsync();
    }

    [TestCaseSource(nameof(UsersUnderTest))]
    public async Task Checkout_CompleteOrder_ShowsConfirmation(string user)
    {
        var inventory = await LoginAsAsync(user);
        await inventory.AddToCartAsync(Products.Backpack);
        var cart = await inventory.Header.OpenCartAsync();
        var information = await cart.ProceedToCheckoutAsync();

        var overview = await information.ContinueWithAsync(CustomerInfo.Valid());
        await Expect(overview.Title).ToHaveTextAsync("Checkout: Overview");

        var complete = await overview.FinishAsync();
        await Expect(complete.CompleteHeader).ToHaveTextAsync("Thank you for your order!");
    }
}
