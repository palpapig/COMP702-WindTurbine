using benchmarking_experimenting.database;
using benchmarking_experimenting.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.Identity.Client.TelemetryCore.TelemetryClient;

namespace benchmarking_experimenting.services;

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
        
        db.TurbineData.Add(telemetry);
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