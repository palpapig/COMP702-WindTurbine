using COMP702_WindTurbine.database;
using COMP702_WindTurbine.models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update.Internal;

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