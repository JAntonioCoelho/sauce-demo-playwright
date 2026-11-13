using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

/// <summary>
/// Regressão visual: as páginas têm de ficar iguais à baseline gerada com o standard_user.
/// O visual_user tem bugs de layout que os testes funcionais não veem (KnownBug).
/// </summary>
[TestFixture]
[Category("Visual")]
public sealed class VisualUserTests : BaseTest
{
    private const string ReferenceUser = Users.Standard;

    private static IEnumerable<TestCaseData> InventoryUsers()
    {
        yield return new TestCaseData(ReferenceUser);
        yield return new TestCaseData(Users.Visual).SetCategory(TestCategories.KnownBug);
    }

    // O detalhe de produto do visual_user ficou igual à baseline: não é KnownBug, é regressão normal.
    private static IEnumerable<TestCaseData> ProductDetailsUsers()
    {
        yield return new TestCaseData(ReferenceUser);
        yield return new TestCaseData(Users.Visual);
    }

    [TestCaseSource(nameof(InventoryUsers))]
    public async Task Inventory_LooksLikeBaseline(string user)
    {
        await LoginAsAsync(user);

        await VisualBaseline.AssertMatchesAsync(Page, "inventory", isReference: user == ReferenceUser);
    }

    [TestCaseSource(nameof(ProductDetailsUsers))]
    public async Task ProductDetails_LooksLikeBaseline(string user)
    {
        var inventory = await LoginAsAsync(user);
        await inventory.OpenProductDetailsAsync(Products.Backpack);

        await VisualBaseline.AssertMatchesAsync(Page, "product-details-backpack", isReference: user == ReferenceUser);
    }
}
