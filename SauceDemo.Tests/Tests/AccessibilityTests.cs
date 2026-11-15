using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

/// <summary>
/// Acessibilidade (WCAG 2.x A/AA) com axe-core em todas as páginas do fluxo de compra.
/// Hoje o Sauce Demo não tem violações WCAG nestas páginas, por isso o gate exige zero:
/// qualquer violação que apareça é uma regressão.
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
    public async Task Page_HasNoAccessibilityViolations(string pageName)
    {
        await NavigateToAsync(pageName);

        var violations = await AccessibilityScanner.ScanAsync(Page, pageName);

        Assert.That(violations, Is.Empty, () => AccessibilityScanner.Describe(pageName, violations));
    }

    /// <summary>
    /// Controlo positivo: um teste que nunca falha pode simplesmente não estar a verificar nada.
    /// Esta página tem violações WCAG conhecidas, e o scanner tem de as encontrar todas.
    /// </summary>
    [Test]
    public async Task Scanner_OnPageWithKnownViolations_ReportsThem()
    {
        await Page.SetContentAsync("""
            <!doctype html>
            <html>
              <head><title>Controlo positivo</title></head>
              <body>
                <main>
                  <img src="data:image/gif;base64,R0lGODlhAQABAAAAACw=">
                  <button></button>
                  <input type="text">
                </main>
              </body>
            </html>
            """);

        var violations = await AccessibilityScanner.ScanAsync(Page, "controlo-positivo");

        Assert.That(
            violations.Select(violation => violation.RuleId),
            Is.SupersetOf(new[] { "html-has-lang", "image-alt", "button-name", "label" }));
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
