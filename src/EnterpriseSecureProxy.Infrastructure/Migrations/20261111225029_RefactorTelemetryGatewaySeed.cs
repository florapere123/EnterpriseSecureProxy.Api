using EnterpriseSecureProxy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseSecureProxy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorTelemetryGatewaySeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            TelemetryGatewaySeedMigrationBuilder.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
