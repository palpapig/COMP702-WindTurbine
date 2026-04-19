namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;


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
        if (turbine.Model is null)
        {
            logger.LogError("Attempted to benchmark a turbine with no assigned TurbineModel");
            return;
        }
        TurbineModel turbineModel = turbine.Model;

        telemetry = Preprocess(telemetry);

        //These bins have windspeed as the key and Power and Frequency as the values respectively
        var (actualBins, frequencyBins) = BinTelemetry(telemetry, turbineModel.CutInWindSpeed, turbineModel.CutOutWindSpeed); 


        ICollection<PowerCurveBin> expectedBins = turbineModel.PowerCurveBins;
        ICollection<PowerCurveBin> deviationBins = [];
        float weightedTotalDeviation = 0f;

        //for each bin, get the ratio between actual power and expected power.
        //Scale that deviation ratio by the bin's frequency and add it to a running total.
        foreach (float windSpeed in actualBins.Keys)
        {   
            try
            {
                float expectedPower = expectedBins.First(bin => bin.WindSpeed == windSpeed).Power;
                float actualPower = actualBins[windSpeed]; 
                float deviationRatio = actualPower / expectedPower;

                deviationBins.Add(new PowerCurveBin
                {
                    WindSpeed = windSpeed,
                    Power = deviationRatio
                });

                float binWeight = frequencyBins[windSpeed] / telemetry.Count;
                weightedTotalDeviation += binWeight * deviationRatio;
            } catch {}
        }

        //convert dictionary to PowerCurveBin list
        ICollection<PowerCurveBin> actualPowerBins = (ICollection<PowerCurveBin>)actualBins.Select(pair => new PowerCurveBin { WindSpeed = pair.Key, Power = pair.Value });
        
        BenchmarkResult result = new BenchmarkResult
        {
            DeviationScore = weightedTotalDeviation,
            DeviationBins = deviationBins,
            PowerBins = actualPowerBins,
            TimeRangeStart = telemetry.Min(row => row.Timestamp),
            TimeRangeEnd = telemetry.Max(row => row.Timestamp),
            TurbineId = turbine.TurbineId
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
        

        for (float i = minBin; i <= maxBin; i += binInterval)
        {   
            float lowerBound = i - (binInterval/2);
            float upperBound = i + (binInterval/2);
            
            //filter for rows in bin
            ICollection<TurbineTelemetry> binRows = [.. telemetry.Where(row => lowerBound < row.CorrectedWindSpeed && row.CorrectedWindSpeed < upperBound )];

            float binnedPower = (float) binRows.Average(row => row.PowerOutput);
            binnedTelemetry[i] = binnedPower;
            binnedFrequency[i] = binRows.Count;
        }

        return (binnedTelemetry, binnedFrequency);
    }

}