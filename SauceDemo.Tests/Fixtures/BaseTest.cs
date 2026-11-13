using NUnit.Framework.Interfaces;
using SauceDemo.Tests.Config;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Fixtures;

/// <summary>
/// Base de todos os testes de UI. Cada teste recebe um BrowserContext novo (isolamento total),
/// e o trace só é guardado quando o teste falha.
/// </summary>
public abstract class BaseTest : PageTest
{
    protected static TestSettings Settings => TestSettings.Current;

    public override BrowserNewContextOptions ContextOptions() => new()
    {
        BaseURL = Settings.BaseUrl,
        Locale = "en-US",
        ViewportSize = new() { Width = 1366, Height = 768 },
    };

    [SetUp]
    public async Task StartTracingAsync()
    {
        await Context.Tracing.StartAsync(new()
        {
            Title = TestContext.CurrentContext.Test.FullName,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task StopTracingAsync()
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
        {
            await Context.Tracing.StopAsync();
            return;
        }

        var tracePath = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "playwright-traces",
            $"{FileNames.ToSafe(TestContext.CurrentContext.Test.FullName)}.zip");

        await Context.Tracing.StopAsync(new() { Path = tracePath });
        TestContext.AddTestAttachment(tracePath, "Playwright trace (abrir em trace.playwright.dev)");
    }

    /// <summary>Login pela UI com um utilizador específico (sem reutilizar o storage state).</summary>
    protected async Task<InventoryPage> LoginAsAsync(string username)
    {
        var login = await new LoginPage(Page).GotoAsync();
        var inventory = await login.LoginSuccessfullyAsync(username, Settings.DefaultPassword);
        await Expect(inventory.Items).ToHaveCountAsync(Products.TotalInCatalog);
        return inventory;
    }
}
