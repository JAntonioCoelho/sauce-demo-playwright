using System.Text.RegularExpressions;
using SauceDemo.Tests.Fixtures;
using SauceDemo.Tests.Pages;
using SauceDemo.Tests.TestData;

namespace SauceDemo.Tests.Tests;

[TestFixture]
[Category("Login")]
public sealed class LoginTests : BaseTest
{
    private LoginPage _loginPage = null!;

    [SetUp]
    public async Task OpenLoginPageAsync() => _loginPage = await new LoginPage(Page).GotoAsync();

    [Test]
    [Category("Smoke")]
    public async Task Login_WithValidCredentials_RedirectsToInventory()
    {
        var inventory = await _loginPage.LoginSuccessfullyAsync(Users.Standard, Settings.DefaultPassword);

        await Expect(Page).ToHaveURLAsync(new Regex("/inventory\\.html$"));
        await Expect(inventory.Title).ToHaveTextAsync("Products");
    }

    [Test]
    public async Task Login_WithLockedOutUser_ShowsLockedOutError()
    {
        await _loginPage.LoginAsync(Users.LockedOut, Settings.DefaultPassword);

        await Expect(_loginPage.ErrorMessage).ToContainTextAsync("this user has been locked out");
        await Expect(Page).ToHaveURLAsync(new Regex("saucedemo\\.com/?$"));
    }

    [TestCase("", "any_password", "Username is required", TestName = "Login_WithoutUsername_ShowsError")]
    [TestCase(Users.Standard, "", "Password is required", TestName = "Login_WithoutPassword_ShowsError")]
    [TestCase(Users.Standard, "wrong_password", "Username and password do not match", TestName = "Login_WithWrongPassword_ShowsError")]
    public async Task Login_WithInvalidInput_ShowsValidationError(string username, string password, string expectedError)
    {
        await _loginPage.LoginAsync(username, password);

        await Expect(_loginPage.ErrorMessage).ToContainTextAsync(expectedError);
    }
}
