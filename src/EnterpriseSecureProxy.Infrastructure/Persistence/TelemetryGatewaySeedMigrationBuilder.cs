using Microsoft.EntityFrameworkCore.Migrations;

namespace EnterpriseSecureProxy.Infrastructure.Persistence;

/// <summary>EF migration helper that replaces legacy defense seed rows with Telemetry Gateway configuration.</summary>
public static class TelemetryGatewaySeedMigrationBuilder
{
    public static void Apply(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM [HeaderRules];
            DELETE FROM [ParameterRules];
            DELETE FROM [ProxyOperations];
            DELETE FROM [Destinations];
            """);

        migrationBuilder.InsertData(
            table: "Destinations",
            columns: new[] { "Id", "Name", "BaseUrl", "IsActive", "TimeoutMilliseconds", "CreatedAt", "UpdatedAt" },
            values: new object[]
            {
                TelemetryGatewaySeedData.RadarDestinationId,
                "telemetry.radar-feed",
                "https://telemetry.internal:8443",
                true,
                15_000,
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        migrationBuilder.InsertData(
            table: "ProxyOperations",
            columns: new[]
            {
                "Id", "OperationKey", "DestinationId", "HttpMethod", "RelativePath", "IsActive",
                "MaxRequestSize", "RequiresEncryption", "AllowedRoles"
            },
            values: new object[,]
            {
                {
                    TelemetryGatewaySeedData.SensorReadingOpId, "sensor.reading.get", TelemetryGatewaySeedData.RadarDestinationId,
                    "GET", "/api/v1/sensors/reading", true, 8192, false, "SensorClient"
                },
                {
                    TelemetryGatewaySeedData.PlatformStatusOpId, "platform.status.get", TelemetryGatewaySeedData.RadarDestinationId,
                    "GET", "/api/v1/platforms/status", true, 8192, false, "PlatformViewer"
                },
                {
                    TelemetryGatewaySeedData.AlertSubscribeOpId, "alert.subscribe", TelemetryGatewaySeedData.RadarDestinationId,
                    "POST", "/api/v1/alerts/subscribe", true, 1024, true, "AlertManager"
                }
            });

        migrationBuilder.InsertData(
            table: "ParameterRules",
            columns: new[] { "Id", "OperationId", "ParameterName", "IsRequired", "DataType", "Location", "MaxLength", "AllowedValues", "Pattern" },
            values: new object?[,]
            {
                {
                    Guid.Parse("c3000000-0000-0000-0000-000000000001"), TelemetryGatewaySeedData.SensorReadingOpId,
                    "sensorId", true, "String", "Query", 64, null, "^[A-Za-z0-9_-]{2,64}$"
                },
                {
                    Guid.Parse("c3000000-0000-0000-0000-000000000002"), TelemetryGatewaySeedData.PlatformStatusOpId,
                    "platformId", true, "String", "Query", 64, null, null
                },
                {
                    Guid.Parse("c3000000-0000-0000-0000-000000000003"), TelemetryGatewaySeedData.AlertSubscribeOpId,
                    "channel", true, "String", "Body", 128, null, null
                }
            });

        migrationBuilder.InsertData(
            table: "HeaderRules",
            columns: new[] { "Id", "OperationId", "HeaderName", "Mode", "InjectedValue", "IsRequired" },
            values: new object?[,]
            {
                { Guid.Parse("d4000000-0000-0000-0000-000000000001"), TelemetryGatewaySeedData.SensorReadingOpId, "X-Correlation-Id", "Forward", null, false },
                { Guid.Parse("d4000000-0000-0000-0000-000000000002"), TelemetryGatewaySeedData.PlatformStatusOpId, "X-Correlation-Id", "Forward", null, false },
                { Guid.Parse("d4000000-0000-0000-0000-000000000003"), TelemetryGatewaySeedData.AlertSubscribeOpId, "X-Correlation-Id", "Forward", null, false }
            });
    }
}
