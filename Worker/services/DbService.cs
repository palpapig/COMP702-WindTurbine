using System.Diagnostics.CodeAnalysis;
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

    //TODO replace all calls of this with directly from csv
    public async Task<List<TurbineTelemetry>> GetFirstYearTurbineData(string turbineId)
    {
        var earliestTimestamp = await db.TurbineData
        .Where(x => x.TurbineId == turbineId)
        .MinAsync(x => x.Timestamp);

        var latestTimestamp = earliestTimestamp.AddYears(1);

        return await db.TurbineData
        .Where(t => t.TurbineId == turbineId && t.Timestamp < latestTimestamp)
        .ToListAsync();
        

    }

    //TODO replace all calls of this with directly from csv
    public async Task<List<TurbineTelemetry>> GetTurbineDataYear(string turbineId, int year)
    {
        return await db.TurbineData
        .Where(t => t.TurbineId == turbineId && t.Timestamp.Year == year)
        .ToListAsync();
    }
    
    public async Task<Turbine> GetTurbineById(string turbineId)
    {
        return await db.Turbine
            .Include(t => t.DegradationModelDetails)
            .Include(t => t.DegradationResults)
            .Include(t => t.TurbineModel)
                .ThenInclude(tm => tm.ExpectedPowerBins)
            .FirstAsync(t => t.TurbineId == turbineId);
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
            db.Turbine.Add(turbine);
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

    public async Task AddDegradationModelDetails(DegradationModelDetails dmd)
    {
        db.Set<Turbine>().Attach(dmd.Turbine); //indicates to not re-add the turbine as a new row
        db.Set<DegradationModelDetails>().Add(dmd);
        await db.SaveChangesAsync();
    }

    public async Task AddDegradationResult(DegradationResult result)
    {
        db.Set<DegradationResult>().Add(result);
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
