namespace benchmarking_experimenting.database;

using benchmarking_experimenting.models;
using Microsoft.EntityFrameworkCore;

public class MonitoringDbContext : DbContext
{
    public DbSet<TurbineTelemetry> TurbineData { get; set; }

    public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
        : base(options)
    {
    }
}
