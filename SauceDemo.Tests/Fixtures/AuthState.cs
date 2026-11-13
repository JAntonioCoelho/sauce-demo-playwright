namespace SauceDemo.Tests.Fixtures;

/// <summary>Caminhos dos ficheiros de storage state gerados no setup global.</summary>
public static class AuthState
{
    public static string StandardUser { get; } =
        Path.Combine(AppContext.BaseDirectory, ".auth", "standard_user.json");
}
