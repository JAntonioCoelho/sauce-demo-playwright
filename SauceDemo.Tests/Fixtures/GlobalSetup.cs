using SauceDemo.Tests.Config;
using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

// Namespace raiz de propósito: o SetUpFixture aplica-se a todos os testes neste namespace e nos filhos.
namespace SauceDemo.Tests;

[SetUpFixture]
public sealed class GlobalSetup
{
    [OneTimeSetUp]
    public async Task CreateAuthenticatedStateAsync()
    {
        var settings = TestSettings.Current;

        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        await using var browser = await playwright.Chromium.LaunchAsync();
        await using var context = await browser.NewContextAsync(new() { BaseURL = settings.BaseUrl });
        var page = await context.NewPageAsync();

        var loginPage = await new LoginPage(page).GotoAsync();
        await loginPage.LoginAsync(Users.Standard, settings.DefaultPassword);
        await page.WaitForURLAsync("**/inventory.html");

        Directory.CreateDirectory(Path.GetDirectoryName(AuthState.StandardUser)!);
        await context.StorageStateAsync(new() { Path = AuthState.StandardUser });
    }
}
