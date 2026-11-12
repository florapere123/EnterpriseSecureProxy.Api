using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace EnterpriseSecureProxy.Tests.Support;

/// <summary>
/// Spins up the real API over an in-memory host with the Mock data source.
/// <para>
/// The data source and environment are applied via process environment variables because
/// <c>AddProxyInfrastructure</c> selects its registration branch eagerly from configuration
/// (minimal hosting), before <see cref="WebApplicationFactory{T}"/> config overrides take effect.
/// Factory-based tests therefore run in a single non-parallel collection (see <see cref="WebFactoryCollection"/>).
/// </para>
/// </summary>
public sealed class ProxyWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;
    private readonly Dictionary<string, string?> _overrides;
    private readonly List<(string Key, string? Original)> _envToRestore = new();

    public ProxyWebApplicationFactory(string environment, IDictionary<string, string?>? overrides = null)
    {
        _environment = environment;
        _overrides = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["DataSource"] = "Mock",
            ["Downstream:UseMockHandler"] = "true",
            ["GatewayTrust:TrustGatewayHeaders"] = "false",
            ["Security:MasterKeyBase64"] = "QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVoxMjM0NTY="
        };

        if (overrides is not null)
        {
            foreach (var pair in overrides)
            {
                _overrides[pair.Key] = pair.Value;
            }
        }

        // Applied early so registration-time reads (DataSource) and environment selection see them.
        SetEnv("ASPNETCORE_ENVIRONMENT", _environment);
        SetEnv("DataSource", _overrides["DataSource"]);
        SetEnv("Downstream__UseMockHandler", _overrides["Downstream:UseMockHandler"]);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(_overrides));
    }

    private void SetEnv(string key, string? value)
    {
        _envToRestore.Add((key, Environment.GetEnvironmentVariable(key)));
        Environment.SetEnvironmentVariable(key, value);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            foreach (var (key, original) in _envToRestore)
            {
                Environment.SetEnvironmentVariable(key, original);
            }
        }
    }
}
