namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;

public sealed class PlaceholderSimulatedDatasource
{
    readonly string csvPath = "asdfasdf/data2017-2022.csv";
    readonly int timestampCol = 0;
    readonly int  powerCol = 1;
    public TurbineTelemetry GetData(int monthsDuration, DateTime endDate)
    {
        //need to know how the data is formatted in the csv
        var rows = File.ReadLines(csvPath)
            .Select(line => line.Split(','))
            .Where(r => DateTime.Parse(r[timestampCol]) <= endDate)
            .ToArray();

        for (int i = rows.Length - 1; i >= 1; i--)
        {
            var parts = rows[i];
            if (parts.Length <= powerCol) continue;

            return new TurbineTelemetry
            {
                TurbineId = "BK-TEST-4",
                Timestamp = DateTime.Parse(parts[timestampCol]),
                PowerOutput = double.Parse(parts[powerCol])
            };
        }

        return new TurbineTelemetry { TurbineId = "BK-TEST-4", Timestamp = endDate };
    }
    public TurbineTelemetry FormatData(RawData data)
    {
        var telemetry = new TurbineTelemetry
        {
            TurbineId = data.TurbineId,
            Timestamp = data.Timestamp,
            WindSpeed = data.WSSensor,
            RotorSpeed = data.RSSensor,
            PowerOutput = data.POSensor,
        };

        return telemetry;
    }

}
