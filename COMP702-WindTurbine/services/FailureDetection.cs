namespace benchmarking_experimenting.services;
using benchmarking_experimenting.models;


public sealed class FailureDetection
{
    public TurbineTelemetry DetectFailure(TurbineTelemetry telemetry)
    {
        var rand = new Random();
        telemetry.StartedAlert = rand.Next(0,10) < 3; //30% chance for alert
        return telemetry;
    }
}