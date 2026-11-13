using System.Globalization;
using SauceDemo.Tests.Pages.Components;

namespace SauceDemo.Tests.Pages;

public sealed class InventoryPage(IPage page) : BasePage(page)
{
    public HeaderComponent Header { get; } = new(page);

    private ILocator SortDropdown => Page.ByTestId("product-sort-container");

    public ILocator Items => Page.ByTestId("inventory-item");
    public ILocator ItemNames => Page.ByTestId("inventory-item-name");
    public ILocator ItemPrices => Page.ByTestId("inventory-item-price");

    private ILocator Item(string productName) => Items.Filter(new() { HasText = productName });

    public async Task<InventoryPage> GotoAsync()
    {
        await Page.GotoAsync("/inventory.html");
        return this;
    }

    public async Task OpenProductDetailsAsync(string productName)
    {
        await ItemNames.Filter(new() { HasText = productName }).ClickAsync();
        await Page.WaitForURLAsync("**/inventory-item.html*");
    }

    public Task AddToCartAsync(string productName) =>
        Item(productName).GetByRole(AriaRole.Button, new() { Name = "Add to cart" }).ClickAsync();

    public Task RemoveFromCartAsync(string productName) =>
        Item(productName).GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();

    /// <summary>Escolhe a ordenação e confirma que o dropdown a aceitou.</summary>
    public async Task SortByAsync(SortOption option)
    {
        await SelectSortOptionAsync(option);

        // Garante que a ordenação foi aplicada antes de alguém ler a lista.
        await Assertions.Expect(SortDropdown).ToHaveValueAsync(option.ToOptionValue());
    }

    /// <summary>Só escolhe a opção, sem validar nada: para testes que verificam o que acontece a seguir.</summary>
    public async Task SelectSortOptionAsync(SortOption option) =>
        await SortDropdown.SelectOptionAsync(option.ToOptionValue());

    /// <summary>Nomes dos produtos que mostram o botão "Remove", ou seja, que estão no carrinho.</summary>
    public async Task<IReadOnlyList<string>> GetNamesInCartAsync() =>
        (await Items
            .Filter(new() { Has = Page.GetByRole(AriaRole.Button, new() { Name = "Remove" }) })
            .ByTestId("inventory-item-name")
            .AllInnerTextsAsync())
        .Select(name => name.Trim())
        .ToList();

    public async Task<IReadOnlyList<decimal>> GetPricesAsync()
    {
        var texts = await ItemPrices.AllInnerTextsAsync();
        return texts
            .Select(text => decimal.Parse(text.Trim().TrimStart('$'), CultureInfo.InvariantCulture))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetImageSourcesAsync() =>
        await Items.Locator("img").EvaluateAllAsync<string[]>(
            "images => images.map(image => image.getAttribute('src'))");

    public async Task<IReadOnlyList<string>> GetNamesAsync() =>
        (await ItemNames.AllInnerTextsAsync()).Select(name => name.Trim()).ToList();
}
