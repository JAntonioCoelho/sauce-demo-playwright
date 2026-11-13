using SauceDemo.Tests.Pages;

namespace SauceDemo.Tests.Pages.Components;

/// <summary>Cabeçalho partilhado entre páginas autenticadas.</summary>
public sealed class HeaderComponent(IPage page)
{
    private ILocator CartLink => page.ByTestId("shopping-cart-link");

    public ILocator CartBadge => page.ByTestId("shopping-cart-badge");

    public async Task<CartPage> OpenCartAsync()
    {
        await CartLink.ClickAsync();
        return new CartPage(page);
    }
}
