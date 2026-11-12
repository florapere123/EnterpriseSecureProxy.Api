namespace EnterpriseSecureProxy.Infrastructure.Configuration;

/// <summary>Selectable backing stores for the configuration repository (Strategy Pattern switch).</summary>
public enum DataSourceKind
{
    Mock = 0,
    Sql = 1
}

/// <summary>
/// Helper for interpreting the top-level <c>"DataSource"</c> setting in appsettings.json
/// (a plain string, e.g. <c>"Mock"</c> or <c>"SQL"</c>). Controls which
/// <see cref="EnterpriseSecureProxy.Domain.Interfaces.IConfigurationRepository"/> strategy is
/// registered at composition time. Defaults to Mock for safe, offline-first startup.
/// </summary>
public static class DataSourceOptions
{
    /// <summary>Configuration key read from the root of appsettings.json.</summary>
    public const string Key = "DataSource";

    public static DataSourceKind Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "sql" => DataSourceKind.Sql,
            "mock" => DataSourceKind.Mock,
            _ => DataSourceKind.Mock
        };
}
