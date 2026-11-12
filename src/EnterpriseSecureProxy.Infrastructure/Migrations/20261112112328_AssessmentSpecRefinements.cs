using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseSecureProxy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentSpecRefinements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowHttpClientRetryForDelete",
                table: "ProxyOperations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowUnknownParameters",
                table: "ProxyOperations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TimeoutMilliseconds",
                table: "ProxyOperations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Destinations",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "SettingsJson",
                table: "Destinations",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfigurationHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ChangeSummaryJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PreviousRowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    NewRowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationHistory_EntityType_EntityId_ChangedAtUtc",
                table: "ConfigurationHistory",
                columns: new[] { "EntityType", "EntityId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigurationHistory");

            migrationBuilder.DropColumn(
                name: "AllowHttpClientRetryForDelete",
                table: "ProxyOperations");

            migrationBuilder.DropColumn(
                name: "AllowUnknownParameters",
                table: "ProxyOperations");

            migrationBuilder.DropColumn(
                name: "TimeoutMilliseconds",
                table: "ProxyOperations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "SettingsJson",
                table: "Destinations");
        }
    }
}
