using COMP702_WindTurbine.database;
using COMP702_WindTurbine.models;
using Microsoft.EntityFrameworkCore;

namespace COMP702_WindTurbine.services;

public sealed class DbService (
    MonitoringDbContext db
)
{
    public async Task<List<TurbineTelemetry>> GetTelemetryAsync()
    {
        return await db.TurbineData.ToListAsync();
    }

    public async Task AddTelemetryAsync(TurbineTelemetry telemetry)
    {
        var turbine = await db.Set<Turbine>().FindAsync(telemetry.TurbineId);
        if (turbine is null)
        {
            turbine = new Turbine
            {
                TurbineId = telemetry.TurbineId,
                Name = telemetry.TurbineId,
                Location = "unknown",
                Status = telemetry.StartedAlert == true ? "Alarm" : "Running",
                LastTelemetryTime = telemetry.Timestamp
            };
            db.Set<Turbine>().Add(turbine);
        }
        else
        {
            turbine.LastTelemetryTime = telemetry.Timestamp;
            turbine.Status = telemetry.StartedAlert == true ? "Alarm" : "Running";
        }

        db.TurbineData.Add(telemetry);
        await db.SaveChangesAsync();
    }

    public async Task AddBenchmarkResultAsync(BenchmarkResult result)
    {
        db.Set<BenchmarkResult>().Add(result);
        await db.SaveChangesAsync();
    }

    public async Task PrintDbAsync()
    {
        var data = await db.TurbineData.ToListAsync();
        foreach (var row in data)
        {
            Console.WriteLine($"Data {row.Id}: {row.PowerOutput}");
        }
    }
}
