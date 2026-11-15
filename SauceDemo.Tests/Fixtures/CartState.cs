using SauceDemo.Tests.Config;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Fixtures;

/// <summary>
/// Prepara o carrinho sem passar pela UI. O Sauce Demo não tem API: guarda o carrinho no localStorage
/// (<c>cart-contents</c>, uma lista de ids de produto). Escrever aí é o equivalente a preparar dados por API:
/// os testes que só precisam de um carrinho cheio como pré-condição não repetem os cliques de "Add to cart",
/// que já são testados nos testes do inventário e do carrinho.
/// </summary>
public static class CartState
{
    public static async Task SeedAsync(IPage page, params string[] products)
    {
        var ids = products.Select(product => Products.Ids[product]).ToArray();

        // O localStorage é por origem: é preciso estar numa página do Sauce Demo para lhe escrever.
        if (!page.Url.StartsWith(TestSettings.Current.BaseUrl, StringComparison.Ordinal))
        {
            await page.GotoAsync("/inventory.html");
        }

        await page.EvaluateAsync("ids => localStorage.setItem('cart-contents', JSON.stringify(ids))", ids);
    }
}
