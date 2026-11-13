namespace SauceDemo.Tests.Fixtures;

/// <summary>
/// Base para testes que precisam de sessão iniciada. Reutiliza o storage state criado
/// uma única vez no <see cref="GlobalSetup"/>, sem repetir o login pela UI em cada teste.
/// </summary>
public abstract class AuthenticatedTest : BaseTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.StorageStatePath = AuthState.StandardUser;
        return options;
    }
}
