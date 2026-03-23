namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;

public sealed class DataInput
{

    public RawData GetDataRow()
    {
        var rand = new Random();
        var rawData = new RawData
        {
            TurbineId = "v0",
            Timestamp = DateTime.Now,
            WSSensor = rand.Next(0,100),
            RSSensor = rand.Next(0,20),
            POSensor = rand.Next(0,20)
        };
        
        return rawData;
    }
}