using COMP702_WindTurbine.Models;
using Microsoft.EntityFrameworkCore;

namespace COMP702_WindTurbine.Infrastructure;

public sealed class MonitoringDbContext : DbContext
{
    public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options) : base(options)
    {
    }

    public DbSet<Turbine> Turbines => Set<Turbine>();
    public DbSet<TelemetryHistory> TelemetryHistories => Set<TelemetryHistory>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<WorkerStatus> WorkerStatuses => Set<WorkerStatus>();
    public DbSet<WorkerMetrics> WorkerMetrics => Set<WorkerMetrics>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Turbine>(entity =>
        {
            entity.ToTable("Turbines");
            entity.HasKey(x => x.TurbineId);

            entity.Property(x => x.TurbineId).HasMaxLength(64);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Location).IsRequired().HasMaxLength(256);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(32);

            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.LastTelemetryTime);
        });

        modelBuilder.Entity<TelemetryHistory>(entity =>
        {
            entity.ToTable("TelemetryHistories");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TurbineId).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Timestamp).IsRequired();

            entity.HasIndex(x => new { x.TurbineId, x.Timestamp });

            entity.HasOne(x => x.Turbine)
                .WithMany(x => x.TelemetryHistories)
                .HasForeignKey(x => x.TurbineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Alert>(entity =>
        {
            entity.ToTable("Alerts");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.TurbineId).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Timestamp).IsRequired();
            entity.Property(x => x.Type).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Severity).IsRequired().HasMaxLength(16);
            entity.Property(x => x.Status).IsRequired().HasMaxLength(16);
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.Property(x => x.AcknowledgedAt);
            entity.Property(x => x.ResolvedAt);
            entity.Property(x => x.ClearedAt);

            entity.HasIndex(x => new { x.TurbineId, x.Timestamp });
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.TurbineId, x.Type, x.Status });

            entity.HasOne(x => x.Turbine)
                .WithMany(x => x.Alerts)
                .HasForeignKey(x => x.TurbineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkerStatus>(entity =>
        {
            entity.ToTable("WorkerStatuses");
            entity.HasKey(x => x.WorkerId);

            entity.Property(x => x.WorkerId).HasMaxLength(64);
            entity.Property(x => x.LastHeartbeat).IsRequired();
            entity.Property(x => x.Status).IsRequired().HasMaxLength(16);
            entity.Property(x => x.LastError).HasMaxLength(1024);

            entity.HasIndex(x => x.LastHeartbeat);
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<WorkerMetrics>(entity =>
        {
            entity.ToTable("WorkerMetrics");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.WorkerId).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Timestamp).IsRequired();

            entity.HasIndex(x => new { x.WorkerId, x.Timestamp });
        });
    }
}
