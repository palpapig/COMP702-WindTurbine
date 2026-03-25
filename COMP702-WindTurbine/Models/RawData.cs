namespace COMP702_WindTurbine.models;

public sealed class RawData
{
    public required string TurbineId { get; init; }
    public DateTime Timestamp { get; init; }
    //core fields from simulation– not required because worker sets them via computed properties
    public double WindSpeed { get; init; } // m/s
    public double ActivePower { get; init; } // kW
    public double RotorSpeed { get; init; } // RPM
    public double PitchAngle { get; init; } // degrees
    public double GearboxOilTemp { get; init; } // °C
    public double Vibration { get; init; }
    public double Temperature { get; init; }

    //Legacy properties for compatibility with existing formatter
    public double WSSensor => WindSpeed;
    public double RSSensor => RotorSpeed;
    public double POSensor => ActivePower;
    public double VibSensor => Vibration;
    public double TempSensor => Temperature;
}
