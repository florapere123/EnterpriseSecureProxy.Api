using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseSecureProxy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OperationKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DestinationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Forwarded = table.Column<bool>(type: "bit", nullable: false),
                    UpstreamStatusCode = table.Column<int>(type: "int", nullable: true),
                    ProxyStatusCode = table.Column<int>(type: "int", nullable: true),
                    OutcomeCategory = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Destinations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BaseUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TimeoutMilliseconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Destinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProxyOperations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DestinationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MaxRequestSize = table.Column<int>(type: "int", nullable: false),
                    RequiresEncryption = table.Column<bool>(type: "bit", nullable: false),
                    AllowedRoles = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProxyOperations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProxyOperations_Destinations_DestinationId",
                        column: x => x.DestinationId,
                        principalTable: "Destinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HeaderRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HeaderName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    InjectedValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeaderRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeaderRules_ProxyOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "ProxyOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParameterRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    AllowedValues = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Pattern = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParameterRules_ProxyOperations_OperationId",
                        column: x => x.OperationId,
                        principalTable: "ProxyOperations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CorrelationId",
                table: "AuditLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_StartTime",
                table: "AuditLogs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Destinations_Name",
                table: "Destinations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeaderRules_OperationId",
                table: "HeaderRules",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ParameterRules_OperationId",
                table: "ParameterRules",
                column: "OperationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyOperations_DestinationId",
                table: "ProxyOperations",
                column: "DestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProxyOperations_OperationKey",
                table: "ProxyOperations",
                column: "OperationKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "HeaderRules");

            migrationBuilder.DropTable(
                name: "ParameterRules");

            migrationBuilder.DropTable(
                name: "ProxyOperations");

            migrationBuilder.DropTable(
                name: "Destinations");
        }
    }
}
