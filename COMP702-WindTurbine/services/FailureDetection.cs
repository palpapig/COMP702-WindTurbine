namespace COMP702_WindTurbine.services;

using COMP702_WindTurbine.models;


public sealed class FailureDetection
{
    // PLACEHOLDER LOGIC – based on outlier removal and fault diagnosis documents
    // To be replaced with real ML model (e.g. kNN with Bagging, EWMA residual control).
    public TurbineTelemetry DetectFailure(TurbineTelemetry telemetry)
    {
        bool isFault = false;

        // 1. Extreme outliers (Outlier Detection doc)
        if (telemetry.PowerOutput < 0) isFault = true;
        if (telemetry.PitchAngle > 20) isFault = true;
        if (telemetry.RotorSpeed < 11) isFault = true;

        // 2. Gearbox oil sump temperature > 65°C (Fault Diagnosis doc)
        if (telemetry.GearboxOilTemp > 65) isFault = true;

        // 3. Very low efficiency (<20%) – adds logical consistency
        if (telemetry.Efficiency < 20) isFault = true;

        telemetry.StartedAlert = isFault;
        return telemetry;
    }
}