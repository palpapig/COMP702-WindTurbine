using COMP702_WindTurbine.models;
using Microsoft.Extensions.Configuration;
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace COMP702_WindTurbine.services;

public sealed class SupabaseService
{
    private readonly Client _supabaseClient;

    public SupabaseService(IConfiguration configuration)
    {
        var url = configuration["Supabase:Url"];
        var key = configuration["Supabase:Key"];
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Supabase URL or Key missing in configuration.");
        _supabaseClient = new Client(url, key);
    }

    public async Task AddTelemetryAsync(TurbineTelemetry telemetry)
    {
        var record = new SupabaseTelemetryRecord
        {
            TurbineId = telemetry.TurbineId,
            Timestamp = telemetry.Timestamp,
            WindSpeed = telemetry.WindSpeed,
            RotorSpeed = telemetry.RotorSpeed,
            PowerOutput = telemetry.PowerOutput,
            Vibration = telemetry.Vibration,
            Temperature = telemetry.Temperature,
            Efficiency = telemetry.Efficiency,
            StartedAlert = telemetry.StartedAlert,
            GearboxOilTemp = telemetry.GearboxOilTemp,
            PitchAngle = telemetry.PitchAngle
        };
        await _supabaseClient.From<SupabaseTelemetryRecord>().Insert(record);
    }
}

// Model must inherit from BaseModel and map to the "TurbineData" table
[Table("TurbineData")]
public class SupabaseTelemetryRecord : BaseModel
{
    [Column("TurbineId")]
    public string? TurbineId { get; set; }

    [Column("Timestamp")]
    public DateTime Timestamp { get; set; }

    [Column("WindSpeed")]
    public double? WindSpeed { get; set; }

    [Column("RotorSpeed")]
    public double? RotorSpeed { get; set; }

    [Column("PowerOutput")]
    public double? PowerOutput { get; set; }

    [Column("Vibration")]
    public double? Vibration { get; set; }

    [Column("Temperature")]
    public double? Temperature { get; set; }

    [Column("Efficiency")]
    public double? Efficiency { get; set; }

    [Column("StartedAlert")]
    public bool? StartedAlert { get; set; }

    [Column("GearboxOilTemp")]
    public double? GearboxOilTemp { get; set; }

    [Column("PitchAngle")]
    public double? PitchAngle { get; set; }
}