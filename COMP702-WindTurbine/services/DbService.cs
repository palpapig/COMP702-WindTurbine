using COMP702_WindTurbine.database;
using COMP702_WindTurbine.models;
using Microsoft.EntityFrameworkCore;

namespace COMP702_WindTurbine.services;

public sealed class DbService(
    MonitoringDbContext db,
    SupabaseService supabaseService
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

        // Retry logic for Supabase inserts (handles 502 and duplicates)
        int maxRetries = 3;
        int retryDelayMs = 1000;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await supabaseService.AddTelemetryAsync(telemetry);
                break; // success
            }
            catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
            {
                if (ex.Message.Contains("23505")) // duplicate key violation
                {
                    Console.WriteLine($"Duplicate skipped for {telemetry.TurbineId} at {telemetry.Timestamp}");
                    break; // skip this insert, continue to next cycle
                }
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"Supabase insert failed after {maxRetries} attempts: {ex.Message}");
                    throw;
                }
                Console.WriteLine($"Supabase insert attempt {attempt} failed: {ex.Message}. Retrying in {retryDelayMs}ms...");
                await Task.Delay(retryDelayMs);
            }
        }
    }

    // This method is called by MonitoringWorker (do NOT remove)
    public async Task PrintDbAsync()
    {
        var data = await db.TurbineData.ToListAsync();
        foreach (var row in data)
        {
            Console.WriteLine($"Data {row.Id}: {row.PowerOutput}");
        }
    }
}