namespace COMP702_WindTurbine.database;

using COMP702_WindTurbine.models;
using Microsoft.EntityFrameworkCore;

public class MonitoringDbContext : DbContext
{
    public DbSet<TurbineTelemetry> TurbineData { get; set; }
    public DbSet<Turbine> Turbine { get; set; }

    public MonitoringDbContext(DbContextOptions<MonitoringDbContext> options)
        : base(options)
    {
    }
}
