using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

/// <summary>
/// Acessibilidade (WCAG 2.x A/AA) com axe-core em todas as páginas do fluxo de compra.
/// O gate falha com violações novas; as que já existem estão em <see cref="KnownAccessibilityViolations"/>.
/// </summary>
[TestFixture]
[Category("Accessibility")]
public sealed class AccessibilityTests : BaseTest
{
    private static readonly string[] PagesUnderTest =
    [
        "login",
        "inventory",
        "product-details",
        "cart",
        "checkout-information",
        "checkout-overview",
        "checkout-complete",
    ];

    private static IEnumerable<TestCaseData> AllPages() =>
        PagesUnderTest.Select(pageName => new TestCaseData(pageName));

    [TestCaseSource(nameof(AllPages))]
    public async Task Page_HasNoNewAccessibilityViolations(string pageName)
    {
        await NavigateToAsync(pageName);

        var violations = await AccessibilityScanner.ScanAsync(Page, pageName);

        var known = KnownAccessibilityViolations.For(pageName);
        var unexpected = violations.Where(violation => !known.Contains(violation.RuleId)).ToList();
        foreach (var rule in known.Except(violations.Select(violation => violation.RuleId)))
        {
            TestContext.Out.WriteLine($"A11Y {pageName}: a violação conhecida '{rule}' já não aparece. Tira-a da lista.");
        }

        Assert.That(unexpected, Is.Empty, () => AccessibilityScanner.Describe(pageName, unexpected));
    }

    /// <summary>Leva o browser até à página pedida pelo fluxo real (login, carrinho, checkout).</summary>
    private async Task NavigateToAsync(string pageName)
    {
        if (pageName == "login")
        {
            await new LoginPage(Page).GotoAsync();
            await Expect(Page.ByTestId("login-button")).ToBeVisibleAsync();
            return;
        }

        var inventory = await LoginAsAsync(Users.Standard);
        if (pageName == "inventory")
        {
            return;
        }

        if (pageName == "product-details")
        {
            await inventory.OpenProductDetailsAsync(Products.Backpack);
            await Expect(Page.ByTestId("inventory-item-name")).ToHaveTextAsync(Products.Backpack);
            return;
        }

        await inventory.AddToCartAsync(Products.Backpack);
        var cart = await inventory.Header.OpenCartAsync();
        await Expect(cart.Title).ToHaveTextAsync("Your Cart");
        if (pageName == "cart")
        {
            return;
        }

        var information = await cart.ProceedToCheckoutAsync();
        await Expect(information.Title).ToHaveTextAsync("Checkout: Your Information");
        if (pageName == "checkout-information")
        {
            return;
        }

        var overview = await information.ContinueWithAsync(CustomerInfo.Valid());
        await Expect(overview.Title).ToHaveTextAsync("Checkout: Overview");
        if (pageName == "checkout-overview")
        {
            return;
        }

        var complete = await overview.FinishAsync();
        await Expect(complete.CompleteHeader).ToBeVisibleAsync();
        if (pageName != "checkout-complete")
        {
            throw new ArgumentOutOfRangeException(nameof(pageName), pageName, "Página desconhecida.");
        }
    }
}
