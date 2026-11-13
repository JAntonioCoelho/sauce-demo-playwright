using Microsoft.Extensions.Configuration;

namespace SauceDemo.Tests.Config;

/// <summary>
/// Configuração dos testes. Ordem de precedência (a última ganha):
/// appsettings.json → appsettings.{TEST_ENVIRONMENT}.json → variáveis de ambiente SAUCEDEMO_*.
/// Ex.: SAUCEDEMO_BaseUrl=https://staging.exemplo.com
/// </summary>
public sealed class TestSettings
{
    private static readonly Lazy<TestSettings> LazyCurrent = new(Load);

    public static TestSettings Current => LazyCurrent.Value;

    public string BaseUrl { get; set; } = string.Empty;

    public string DefaultPassword { get; set; } = string.Empty;

    private static TestSettings Load()
    {
        var environment = Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") ?? "Local";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables(prefix: "SAUCEDEMO_")
            .Build();

        var settings = configuration.Get<TestSettings>()
            ?? throw new InvalidOperationException("Não foi possível carregar a configuração dos testes.");

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            throw new InvalidOperationException("BaseUrl não está configurado.");
        }

        return settings;
    }
}
