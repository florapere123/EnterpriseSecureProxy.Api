using EnterpriseSecureProxy.Application.Interfaces;
using EnterpriseSecureProxy.Application.Validation;
using EnterpriseSecureProxy.Domain.Interfaces;
using EnterpriseSecureProxy.Infrastructure.Auditing;
using EnterpriseSecureProxy.Infrastructure.Caching;
using EnterpriseSecureProxy.Infrastructure.Configuration;
using EnterpriseSecureProxy.Infrastructure.Forwarding;
using EnterpriseSecureProxy.Infrastructure.Persistence;
using EnterpriseSecureProxy.Infrastructure.Repositories;
using EnterpriseSecureProxy.Infrastructure.Security;
using EnterpriseSecureProxy.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace EnterpriseSecureProxy.Infrastructure.DependencyInjection;

/// <summary>Wires up infrastructure services and picks the Mock or SQL data source from config.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddProxyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dataSource = DataSourceOptions.Parse(configuration[DataSourceOptions.Key]);
        var connectionString = configuration.GetConnectionString("ProxyConfiguration");

        services.Configure<SecuritySettings>(configuration.GetSection(SecuritySettings.SectionName));
        services.Configure<DownstreamOptions>(configuration.GetSection(DownstreamOptions.SectionName));
        services.Configure<GatewayTrustOptions>(configuration.GetSection(GatewayTrustOptions.SectionName));
        services.Configure<ProxyDefaultsOptions>(configuration.GetSection(ProxyDefaultsOptions.SectionName));
        services.AddSingleton<DestinationConcurrencyLimiter>();
        services.AddSingleton<IEncryptionService, AesGcmEncryptionService>();
        services.AddSingleton<OptimisticConcurrencyUpdater>();

        services.AddMemoryCache();
        services.AddSingleton<IIdempotencyResponseCache, MemoryIdempotencyResponseCache>();

        switch (dataSource)
        {
            case DataSourceKind.Sql:
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "DataSource is SQL but ConnectionStrings:ProxyConfiguration is missing.");
                }

                services.AddDbContextFactory<ProxyDbContext>(options =>
                    options.UseSqlServer(connectionString, sql =>
                    {
                        sql.EnableRetryOnFailure(3);
                        sql.CommandTimeout(30);
                    }));

                services.AddSingleton<SqlConfigurationRepository>();
                services.AddSingleton<IConfigurationRepository>(sp => sp.GetRequiredService<SqlConfigurationRepository>());
                services.AddSingleton<IConfigurationCacheInvalidator>(sp => sp.GetRequiredService<SqlConfigurationRepository>());
                services.AddScoped<IProxyOperationAdminService, ProxyOperationAdminService>();
                services.AddScoped<IDestinationAdminService, DestinationAdminService>();

                services.AddSingleton<AuditLogChannel>();
                services.AddSingleton<ChannelAuditSink>();
                services.AddSingleton<IAuditSink>(sp => sp.GetRequiredService<ChannelAuditSink>());
                services.AddHostedService<AuditLogProcessor>();
                break;
            case DataSourceKind.Mock:
            default:
                services.AddSingleton<IConfigurationRepository, MockConfigurationRepository>();
                services.AddSingleton<IConfigurationCacheInvalidator, NullConfigurationCacheInvalidator>();
                services.AddSingleton<IProxyOperationAdminService, MockProxyOperationAdminService>();
                services.AddSingleton<IDestinationAdminService, MockDestinationAdminService>();
                services.AddSingleton<IAuditSink, LoggingAuditSink>();
                break;
        }

        services.AddSingleton<IRequestValidator, ParameterRuleValidator>();

        RegisterForwarderHttpClient(services, configuration);

        services.AddSingleton<IProxyForwarder, HttpProxyForwarder>();

        return services;
    }

    private static void RegisterForwarderHttpClient(IServiceCollection services, IConfiguration configuration)
    {
        var useMockDownstream = configuration.GetSection(DownstreamOptions.SectionName)
            .GetValue(nameof(DownstreamOptions.UseMockHandler), false);

        var builder = services.AddHttpClient(HttpProxyForwarder.HttpClientName);

        if (useMockDownstream)
        {
            builder.ConfigurePrimaryHttpMessageHandler(sp =>
                sp.GetRequiredService<MockDownstreamHttpMessageHandler>());
            services.AddSingleton<MockDownstreamHttpMessageHandler>();
        }

        // GET-class verbs may retry; POST/PUT/PATCH never retry at this layer (see HttpProxyForwarder).
        builder
            .AddPolicyHandler(ForwarderResiliencePolicies.SelectRetryPolicy)
            .AddPolicyHandler(ForwarderResiliencePolicies.BuildCircuitBreakerPolicy());
    }
}
