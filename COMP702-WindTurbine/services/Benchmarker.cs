namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;
public sealed class PowerBin
{
    public required float WindSpeed { get; set; }
    public required float Power { get; set; }
}


public sealed class Benchmarker (
    ILogger<MonitoringWorker> logger
)
{
    public TurbineTelemetry DummyBenchmark(TurbineTelemetry telemetry)
    {
        var rand = new Random();
        telemetry.Efficiency = Math.Round(rand.NextDouble()*100, 2);
        return telemetry;
    }

    public void Benchmark(ICollection<TurbineTelemetry> telemetry, Turbine turbine)
    {
        //Check if turbine model specs exist to benchmark against
        if (turbine.TurbineModel is null)
        {
            logger.LogError("Attempted to benchmark a turbine with no assigned TurbineModel");
            return;
        }
        TurbineModel turbineModel = turbine.TurbineModel;

        //Preprocess
        telemetry = Preprocess(telemetry);



        //For ease of calculation, all power bins are stored as dictionaries with key:value as windspeed:power
        //frequencyBins has key:value as windspeed:frequency
        var (measuredPowerBins, frequencyBins) = BinTelemetry(telemetry, turbineModel.CutInWindSpeed, turbineModel.CutOutWindSpeed); 

        Dictionary<float, float> expectedPowerBins = turbineModel.ExpectedPowerBins.ToDictionary(row => row.WindSpeed, row => row.Power);
        ICollection<PowerBinDeviation> deviationPowerBins = [];
        float weightedTotalDeviation = 0f;

        //each bin, get the ratio between measured power and expected power.
        //Scale that deviation ratio by the bin's frequency and add it to a running total.
        foreach (float windSpeed in measuredPowerBins.Keys)
        {   
            try
            {
                float expectedPower = expectedPowerBins[windSpeed];
                float measuredPower = measuredPowerBins[windSpeed]; 
                float deviationRatio =  measuredPower / expectedPower;
                float deviationDifference =  measuredPower - expectedPower;

                // deviationPowerBins.Add(new PowerBinDeviation
                // {
                //     WindSpeed = windSpeed,
                //     PowerRatio = deviationRatio,
                //     PowerDifference = deviationDifference
                // });

                float binWeight = frequencyBins[windSpeed] / telemetry.Count;
                weightedTotalDeviation += binWeight * deviationRatio;
            } catch {}
        }

        //convert dictionary to PowerBin list
        ICollection<PowerBinMeasured> realMeasuredPowerBins = (ICollection<PowerBinMeasured>)measuredPowerBins.Select(pair => new PowerBin { WindSpeed = pair.Key, Power = pair.Value });
        
        BenchmarkResult result = new()
        {
            DeviationScore = weightedTotalDeviation,
            DeviationBins = deviationPowerBins,
            PowerBins = realMeasuredPowerBins,
            TimeRangeStart = telemetry.Min(row => row.Timestamp),
            TimeRangeEnd = telemetry.Max(row => row.Timestamp),
            Turbine = turbine
        };



    }

    private static ICollection<TurbineTelemetry> Preprocess(ICollection<TurbineTelemetry> telemetry, bool hasCorrectedWindSpeed = true)
    {
        //remove rows with negative minimum power output.
        telemetry = [.. telemetry.Where(row => row.MinimumPowerOutput > 0)];
    
        //TODO binning or SVR to filter by blade pitch angle

        if (!hasCorrectedWindSpeed)
        {
            //TODO do math to convert wind speed and temperature to corrected wind speed
        }

        return telemetry;
    }

    private static (Dictionary<float, float>, Dictionary<float, int>) BinTelemetry(ICollection<TurbineTelemetry> telemetry, float minBin, float maxBin, float binInterval = 0.5f)
    {


        Dictionary<float, float> binnedTelemetry = [];
        Dictionary<float, int> binnedFrequency = [];
        
        //for each bin...
        for (float i = minBin; i <= maxBin; i += binInterval)
        {   
            float lowerBound = i - (binInterval/2);
            float upperBound = i + (binInterval/2);
            
            //filter for rows in bin (where windspeed is in range)
            ICollection<TurbineTelemetry> binRows = [.. telemetry.Where(row => lowerBound < row.CorrectedWindSpeed && row.CorrectedWindSpeed < upperBound )];

            //store average power and # of rows for that windspeed
            float binnedPower = (float) binRows.Average(row => row.PowerOutput);
            binnedTelemetry[i] = binnedPower;
            binnedFrequency[i] = binRows.Count;
        }

        return (binnedTelemetry, binnedFrequency);
    }

}