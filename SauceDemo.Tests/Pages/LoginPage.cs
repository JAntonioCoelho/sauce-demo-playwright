namespace SauceDemo.Tests.Pages;

public sealed class LoginPage(IPage page) : BasePage(page)
{
    private ILocator UsernameInput => Page.ByTestId("username");
    private ILocator PasswordInput => Page.ByTestId("password");
    private ILocator LoginButton => Page.ByTestId("login-button");

    public ILocator ErrorMessage => Page.ByTestId("error");

    public async Task<LoginPage> GotoAsync()
    {
        await Page.GotoAsync("/");
        return this;
    }

    public async Task LoginAsync(string username, string password)
    {
        await UsernameInput.FillAsync(username);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
    }

    public async Task<InventoryPage> LoginSuccessfullyAsync(string username, string password)
    {
        await LoginAsync(username, password);
        return new InventoryPage(Page);
    }
}
