namespace COMP702_WindTurbine.services;

using COMP702_WindTurbine.models;


public sealed class DataFormatter
{
    public TurbineTelemetry FormatData(RawData data)
    {
        var telemetry = new TurbineTelemetry
        {
            TurbineId = data.TurbineId,
            Timestamp = data.Timestamp,
            WindSpeed = data.WSSensor,
            RotorSpeed = data.RSSensor,
            PowerOutput = data.POSensor,

            // fault detection fields
            GearboxOilTemp = data.GearboxOilTemp,
            GearOilInletTemp = data.GearOilInletTemp,
            RearBearingTemp = data.RearBearingTemp,
            GearOilPumpPressure = data.GearOilPumpPressure,
            GeneratorBearingFrontTemp = data.GeneratorBearingFrontTemp,
            GearOilInletPressure = data.GearOilInletPressure,
            NacelleTemp = data.NacelleTemp

        };

        return telemetry;
    }
}
