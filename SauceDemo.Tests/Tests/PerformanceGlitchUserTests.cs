using System.Diagnostics;
using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

/// <summary>
/// O performance_glitch_user responde devagar. A suite tem de aguentar a lentidão sem ficar flaky
/// (sem sleeps: só asserções web-first com um timeout explícito) e, à parte, apanhar a regressão
/// de performance com um orçamento de tempo.
/// </summary>
[TestFixture]
[Category("UserBehaviour")]
public sealed class PerformanceGlitchUserTests : BaseTest
{
    // O standard_user faz login em ~1-2 s; o glitch leva ~6 s. 4 s separa os dois com margem para redes lentas.
    private static readonly TimeSpan LoginBudget = TimeSpan.FromSeconds(4);
    private const float SlowPageTimeoutMs = 15_000;

    private static IEnumerable<TestCaseData> UsersUnderTest()
    {
        yield return new TestCaseData(Users.Standard);
        yield return new TestCaseData(Users.PerformanceGlitch).SetCategory(TestCategories.KnownBug);
    }

    [Test]
    public async Task Login_WithPerformanceGlitchUser_EventuallyReachesInventory()
    {
        var login = await new LoginPage(Page).GotoAsync();

        var inventory = await login.LoginSuccessfullyAsync(Users.PerformanceGlitch, Settings.DefaultPassword);

        await Expect(inventory.Title).ToHaveTextAsync("Products", new() { Timeout = SlowPageTimeoutMs });
        await Expect(inventory.Items).ToHaveCountAsync(Products.TotalInCatalog);
    }

    [TestCaseSource(nameof(UsersUnderTest))]
    public async Task Login_CompletesWithinPerformanceBudget(string user)
    {
        var login = await new LoginPage(Page).GotoAsync();
        var stopwatch = Stopwatch.StartNew();

        var inventory = await login.LoginSuccessfullyAsync(user, Settings.DefaultPassword);
        await Expect(inventory.Title).ToHaveTextAsync("Products", new() { Timeout = SlowPageTimeoutMs });

        stopwatch.Stop();
        TestContext.Out.WriteLine($"Login de {user}: {stopwatch.ElapsedMilliseconds} ms");
        Assert.That(stopwatch.Elapsed, Is.LessThan(LoginBudget),
            $"O login devia ficar abaixo de {LoginBudget.TotalSeconds} s.");
    }
}
