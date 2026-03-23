namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;


public sealed class DataFormatter
{
    private int IdInc = 1;
    public TurbineTelemetry FormatData(RawData data)
    {
        var telemetry = new TurbineTelemetry
        {
            Id = IdInc,
            TurbineId = data.TurbineId,
            Timestamp = data.Timestamp,
            WindSpeed = data.WSSensor,
            RotorSpeed = data.RSSensor,
            PowerOutput = data.POSensor,
        };


        IdInc++;

        return telemetry;
    }
}