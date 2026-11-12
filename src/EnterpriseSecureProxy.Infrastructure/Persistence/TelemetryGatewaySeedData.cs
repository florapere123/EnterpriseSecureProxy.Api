using EnterpriseSecureProxy.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseSecureProxy.Infrastructure.Persistence;

/// <summary>Telemetry/Sensor Gateway reference configuration (rules-only; no operational payloads).</summary>
public static class TelemetryGatewaySeedData
{
    public static readonly Guid RadarDestinationId = Guid.Parse("a1000000-0000-0000-0000-000000000001");

    public static readonly Guid SensorReadingOpId = Guid.Parse("b2000000-0000-0000-0000-000000000001");
    public static readonly Guid PlatformStatusOpId = Guid.Parse("b2000000-0000-0000-0000-000000000002");
    public static readonly Guid AlertSubscribeOpId = Guid.Parse("b2000000-0000-0000-0000-000000000003");

    private static readonly DateTime SeedTimestamp = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    public static async Task ReplaceAllAsync(ProxyDbContext db, CancellationToken cancellationToken = default)
    {
        await db.HeaderRules.ExecuteDeleteAsync(cancellationToken);
        await db.ParameterRules.ExecuteDeleteAsync(cancellationToken);
        await db.ProxyOperations.ExecuteDeleteAsync(cancellationToken);
        await db.Destinations.ExecuteDeleteAsync(cancellationToken);

        await InsertAsync(db, cancellationToken);
    }

    public static async Task InsertIfEmptyAsync(ProxyDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.ProxyOperations.AnyAsync(cancellationToken))
        {
            return;
        }

        await InsertAsync(db, cancellationToken);
    }

    private static async Task InsertAsync(ProxyDbContext db, CancellationToken cancellationToken)
    {
        var destination = new DestinationEntity
        {
            Id = RadarDestinationId,
            Name = "telemetry.radar-feed",
            BaseUrl = "https://telemetry.internal:8443",
            IsActive = true,
            TimeoutMilliseconds = 15_000,
            CreatedAt = SeedTimestamp,
            UpdatedAt = SeedTimestamp
        };

        db.Destinations.Add(destination);

        db.ProxyOperations.AddRange(
            new ProxyOperationEntity
            {
                Id = SensorReadingOpId,
                OperationKey = "sensor.reading.get",
                DestinationId = RadarDestinationId,
                HttpMethod = "GET",
                RelativePath = "/api/v1/sensors/reading",
                IsActive = true,
                MaxRequestSize = 8_192,
                RequiresEncryption = false,
                AllowedRoles = "SensorClient",
                ParameterRules =
                {
                    new ParameterRuleEntity
                    {
                        Id = Guid.Parse("c3000000-0000-0000-0000-000000000001"),
                        OperationId = SensorReadingOpId,
                        ParameterName = "sensorId",
                        IsRequired = true,
                        DataType = "String",
                        Location = "Query",
                        MaxLength = 64,
                        Pattern = "^[A-Za-z0-9_-]{2,64}$"
                    }
                },
                HeaderRules =
                {
                    new HeaderRuleEntity
                    {
                        Id = Guid.Parse("d4000000-0000-0000-0000-000000000001"),
                        OperationId = SensorReadingOpId,
                        HeaderName = "X-Correlation-Id",
                        Mode = "Forward"
                    }
                }
            },
            new ProxyOperationEntity
            {
                Id = PlatformStatusOpId,
                OperationKey = "platform.status.get",
                DestinationId = RadarDestinationId,
                HttpMethod = "GET",
                RelativePath = "/api/v1/platforms/status",
                IsActive = true,
                MaxRequestSize = 8_192,
                RequiresEncryption = false,
                AllowedRoles = "PlatformViewer",
                ParameterRules =
                {
                    new ParameterRuleEntity
                    {
                        Id = Guid.Parse("c3000000-0000-0000-0000-000000000002"),
                        OperationId = PlatformStatusOpId,
                        ParameterName = "platformId",
                        IsRequired = true,
                        DataType = "String",
                        Location = "Query",
                        MaxLength = 64
                    }
                },
                HeaderRules =
                {
                    new HeaderRuleEntity
                    {
                        Id = Guid.Parse("d4000000-0000-0000-0000-000000000002"),
                        OperationId = PlatformStatusOpId,
                        HeaderName = "X-Correlation-Id",
                        Mode = "Forward"
                    }
                }
            },
            new ProxyOperationEntity
            {
                Id = AlertSubscribeOpId,
                OperationKey = "alert.subscribe",
                DestinationId = RadarDestinationId,
                HttpMethod = "POST",
                RelativePath = "/api/v1/alerts/subscribe",
                IsActive = true,
                MaxRequestSize = 1_024,
                RequiresEncryption = true,
                AllowedRoles = "AlertManager",
                ParameterRules =
                {
                    new ParameterRuleEntity
                    {
                        Id = Guid.Parse("c3000000-0000-0000-0000-000000000003"),
                        OperationId = AlertSubscribeOpId,
                        ParameterName = "channel",
                        IsRequired = true,
                        DataType = "String",
                        Location = "Body",
                        MaxLength = 128
                    }
                },
                HeaderRules =
                {
                    new HeaderRuleEntity
                    {
                        Id = Guid.Parse("d4000000-0000-0000-0000-000000000003"),
                        OperationId = AlertSubscribeOpId,
                        HeaderName = "X-Correlation-Id",
                        Mode = "Forward"
                    }
                }
            });

        await db.SaveChangesAsync(cancellationToken);
    }
}
