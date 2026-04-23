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

    public BenchmarkResult Benchmark(ICollection<TurbineTelemetry> telemetry, Turbine turbine)
    {
        logger.LogInformation("Now Benchmarking Turbine {TurbineId}", turbine.TurbineId);
        //Check if turbine model specs exist to benchmark against
        if (turbine.TurbineModel is null)
        {
            logger.LogError("Attempted to benchmark a turbine with no assigned TurbineModel");
            throw new ArgumentNullException(nameof(turbine.TurbineModel));
        }
        TurbineModel turbineModel = turbine.TurbineModel;

        //Preprocess
        telemetry = Preprocess(telemetry);

        //Create result object to be filled in at the end
        BenchmarkResult result = new()
        {
            DeviationBins = [],
            PowerBins = [],
            TimeRangeStart = telemetry.Min(row => row.Timestamp),
            TimeRangeEnd = telemetry.Max(row => row.Timestamp),
            Turbine = turbine
        };

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
            float expectedPower = expectedPowerBins[windSpeed];
            float measuredPower = measuredPowerBins[windSpeed]; 
            float deviationRatio =  measuredPower / expectedPower;
            float deviationDifference =  measuredPower - expectedPower;

            deviationPowerBins.Add(new PowerBinDeviation
            {
                BenchmarkResult = result,
                WindSpeed = windSpeed,
                PowerRatio = deviationRatio,
                PowerDifference = deviationDifference
            });


            float binWeight = (float)frequencyBins[windSpeed] / telemetry.Count;
            weightedTotalDeviation += binWeight * deviationRatio;

        }

        //convert dictionary to PowerBin list
        ICollection<PowerBinMeasured> convertedMeasuredPowerBins = [.. measuredPowerBins.Select(pair => new PowerBinMeasured { WindSpeed = pair.Key, Power = pair.Value, BenchmarkResult = result })];
        
        //fill in benchmark results
        result.DeviationBins = deviationPowerBins;
        result.PowerBins = convertedMeasuredPowerBins;
        result.DeviationScore = weightedTotalDeviation;

        logger.LogInformation("Benchmark of Turbine {turbineId} successful", turbine.TurbineId);
        return result;
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

    private (Dictionary<float, float>, Dictionary<float, int>) BinTelemetry(ICollection<TurbineTelemetry> telemetry, float minBin, float maxBin, float binInterval = 0.5f)
    {


        Dictionary<float, float> binnedTelemetry = [];
        Dictionary<float, int> binnedFrequency = [];
        
        //for each bin...
        for (float i = minBin; i <= maxBin; i += binInterval)
        {   
            float lowerBound = i - (binInterval/2);
            float upperBound = i + (binInterval/2);
            
            //filter for rows in bin (where windspeed is in range)
            ICollection<TurbineTelemetry> binRows = [.. telemetry.Where(row => lowerBound <= row.CorrectedWindSpeed && row.CorrectedWindSpeed < upperBound )];

            if (binRows.Count == 0)
            {
                continue;
            }

            //store average power and # of rows for that windspeed
            float binnedPower = (float) binRows.Average(row => row.PowerOutput);
            binnedTelemetry[i] = binnedPower;
            binnedFrequency[i] = binRows.Count;
        }

        return (binnedTelemetry, binnedFrequency);
    }

}