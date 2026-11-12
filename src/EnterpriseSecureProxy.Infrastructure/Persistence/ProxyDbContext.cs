using EnterpriseSecureProxy.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseSecureProxy.Infrastructure.Persistence;

/// <summary>EF context for proxy configuration, audit logs, and change history.</summary>
public sealed class ProxyDbContext : DbContext
{
    public ProxyDbContext(DbContextOptions<ProxyDbContext> options)
        : base(options)
    {
    }

    public DbSet<DestinationEntity> Destinations => Set<DestinationEntity>();
    public DbSet<ProxyOperationEntity> ProxyOperations => Set<ProxyOperationEntity>();
    public DbSet<ParameterRuleEntity> ParameterRules => Set<ParameterRuleEntity>();
    public DbSet<HeaderRuleEntity> HeaderRules => Set<HeaderRuleEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<ConfigurationHistoryEntity> ConfigurationHistory => Set<ConfigurationHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DestinationEntity>(entity =>
        {
            entity.ToTable("Destinations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.BaseUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.Property(e => e.SettingsJson).HasMaxLength(4000);
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<ProxyOperationEntity>(entity =>
        {
            entity.ToTable("ProxyOperations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OperationKey).HasMaxLength(256).IsRequired();
            entity.HasIndex(e => e.OperationKey).IsUnique();
            entity.Property(e => e.HttpMethod).HasMaxLength(16).IsRequired();
            entity.Property(e => e.RelativePath).HasMaxLength(512).IsRequired();
            entity.Property(e => e.AllowedRoles).HasMaxLength(512);
            entity.Property(e => e.RowVersion).IsRowVersion();

            entity.HasOne(e => e.Destination)
                .WithMany(d => d.Operations)
                .HasForeignKey(e => e.DestinationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ParameterRuleEntity>(entity =>
        {
            entity.ToTable("ParameterRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ParameterName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.DataType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(16).IsRequired();
            entity.Property(e => e.AllowedValues).HasMaxLength(1024);
            entity.Property(e => e.Pattern).HasMaxLength(512);

            entity.HasOne(e => e.Operation)
                .WithMany(o => o.ParameterRules)
                .HasForeignKey(e => e.OperationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<HeaderRuleEntity>(entity =>
        {
            entity.ToTable("HeaderRules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HeaderName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Mode).HasMaxLength(16).IsRequired();
            entity.Property(e => e.InjectedValue).HasMaxLength(1024);

            entity.HasOne(e => e.Operation)
                .WithMany(o => o.HeaderRules)
                .HasForeignKey(e => e.OperationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ConfigurationHistoryEntity>(entity =>
        {
            entity.ToTable("ConfigurationHistory");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).HasMaxLength(64).IsRequired();
            entity.Property(e => e.ChangeSummaryJson).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.ChangedBy).HasMaxLength(256);
            entity.HasIndex(e => new { e.EntityType, e.EntityId, e.ChangedAtUtc });
        });

        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CorrelationId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.OperationKey).HasMaxLength(256).IsRequired();
            entity.Property(e => e.OutcomeCategory).HasMaxLength(64).IsRequired();
            entity.Property(e => e.RejectionReason).HasMaxLength(2000);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.StartTime);
        });
    }
}
