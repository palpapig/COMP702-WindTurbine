namespace COMP702_WindTurbine.services;

using COMP702_WindTurbine.models;
public sealed class PowerBin
{
    public required float WindSpeed { get; set; }
    public required float Power { get; set; }
}


public sealed class Benchmarker(
    ILogger<MonitoringWorker> logger,
    IServiceScopeFactory scopeFactory,
    PlaceholderHistoricalDataSource historicalDataSource)

{

    /// <summary>
    /// <para>Performs benchmarking on the turbine with the given Id and the described time period.
    /// </para>
    /// Turbine information is read directly from Supabase.
    /// Telemetry data is read from PlaceholderHistoricalDataSource.cs, which reads from the same .csv as the live data simulator.
    /// </summary>
    public async Task ForceDoBenchmarking(DateTime endDate, string turbineId = "BK-TEST-4", int monthsGap = 12)
    {
        using (var tempScope = scopeFactory.CreateScope())
        {
            var dbService = tempScope.ServiceProvider.GetRequiredService<DbService>();

            Turbine turbine = await dbService.GetTurbineById(turbineId);

            //Get telemetry to be benchmarked
            List<TurbineTelemetry> benchmarkTelemetry = historicalDataSource.GetHistoricalTurbineData(monthsGap, endDate);

            logger.LogInformation("turbine name: {tname}", turbine.Name);

            if (benchmarkTelemetry.Count == 0)
            {
                logger.LogWarning("Skipping benchmark for turbine {turbineId}: no telemetry found for year {year}", turbineId, endDate.Year);
                return;
            }

            //Actual benchmarking happens in this function here
            BenchmarkResult benchmarkResult = Benchmark(benchmarkTelemetry, turbine);
            await dbService.AddBenchmarkResultAsync(benchmarkResult);
            logger.LogInformation("Successfully written benchmark results to database");

        }
    }

    /// <summary>
    /// <para> Checks if the turbine with the given turbine Id has a BenchmarkResult ending in the last monthsGap months. If not, performs benchmarking on the last monthsGap months of telemetry starting at currentDate 
    /// </para>
    /// Turbine information is read directly from Supabase.
    /// Telemetry data is read from PlaceholderHistoricalDataSource.cs, which reads from the same .csv as the live data simulator.
    /// </summary>
    public async Task DoBenchmarkingIfNeeded(string turbineId, DateTime currentDate, int monthsGap = 12)
    {
        DateTime endDate = currentDate;
        using (var tempScope = scopeFactory.CreateScope())
        {
            var dbService = tempScope.ServiceProvider.GetRequiredService<DbService>();

            Turbine turbine = await dbService.GetTurbineById(turbineId);

            //if there is at least one benchmark result, AND the most recent one happened less than monthGap months ago, don't do analysis
            if (turbine.BenchmarkResults.Count > 0)
            {
                DateTime lastAnalysed = turbine.BenchmarkResults.MaxBy(r => r.TimeRangeEnd).TimeRangeEnd;
                if (lastAnalysed.AddMonths(monthsGap) > currentDate)
                {
                    logger.LogInformation("No benchmarking needed, latest benchmark for turbine {t} happened less than {m} months ago", turbine.TurbineId, monthsGap);
                    return;
                }
            }

            //Get telemetry to be benchmarked
            List<TurbineTelemetry> recentTelemetry = historicalDataSource.GetHistoricalTurbineData(monthsGap, endDate);

            if (recentTelemetry.Count == 0)
            {
                logger.LogWarning("Attempted to do degradation analysis on turbine {t}, but no data exists for given time period", turbine.TurbineId);
                return;
            }

            //Actual benchmarking happens in this function here
            BenchmarkResult benchmarkResult = Benchmark(recentTelemetry, turbine);
            if (benchmarkResult != null)
            {
                await dbService.AddBenchmarkResultAsync(benchmarkResult);
                logger.LogInformation("Successfully written benchmark results to database");
            }

        }
    }


    /// <summary>
    /// <para>The core function which actually performs the benchmarking. Benchmarks a given Turbine with it's Telemetry.
    /// Provided Telemetry is assumed to be owned by the Turbine. 
    /// </para>
    /// Bins the power output of the telemetry into 0.5 m/s windSpeed bins then compares it to the expected power bins from the Turbine's TurbineModel to get the deviation at each wind speed.
    /// Also averages the deviations to get a single benchmark score for the given time period.
    /// Stores the information in a BenchmarkResult and returns it.
    /// </summary>

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
        if (telemetry.Count == 0)
        {
            throw new InvalidOperationException($"Cannot benchmark turbine {turbine.TurbineId}: no telemetry remains after preprocessing.");
        }

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
            float deviationRatio = measuredPower / expectedPower;
            float deviationDifference = measuredPower - expectedPower;

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
        weightedTotalDeviation = (weightedTotalDeviation - 1) * 100; //become percentage difference 

        //convert dictionary to PowerBin list
        ICollection<PowerBinMeasured> convertedMeasuredPowerBins = [.. measuredPowerBins.Select(pair => new PowerBinMeasured { WindSpeed = pair.Key, Power = pair.Value, BenchmarkResult = result })];

        //fill in benchmark results
        result.DeviationBins = deviationPowerBins;
        result.PowerBins = convertedMeasuredPowerBins;
        result.DeviationScore = weightedTotalDeviation;

        logger.LogInformation("Benchmark of Turbine {turbineId} successful", turbine.TurbineId);
        return result;
    }

    /// <summary>
    /// Removes Telemetry with a minimum power output below 0
    /// </summary>

    public static ICollection<TurbineTelemetry> Preprocess(ICollection<TurbineTelemetry> telemetry, bool hasCorrectedWindSpeed = true)
    {
        //remove rows with negative minimum power output.
        telemetry = [.. telemetry.Where(row => row.MinimumPowerOutput > 0)];

        return telemetry;
    }

    /// <summary>
    /// Helper function to convert a Telemetry list into Power-WindSpeed bins, and each bin's frequency.
    /// </summary>
    private (Dictionary<float, float>, Dictionary<float, int>) BinTelemetry(ICollection<TurbineTelemetry> telemetry, float minBin, float maxBin, float binInterval = 0.5f)
    {


        Dictionary<float, float> binnedTelemetry = [];
        Dictionary<float, int> binnedFrequency = [];

        //for each bin...
        for (float i = minBin; i <= maxBin; i += binInterval)
        {
            float lowerBound = i - (binInterval / 2);
            float upperBound = i + (binInterval / 2);

            //filter for rows in bin (where windspeed is in range)
            ICollection<TurbineTelemetry> binRows = [.. telemetry.Where(row => lowerBound <= row.CorrectedWindSpeed && row.CorrectedWindSpeed < upperBound)];

            if (binRows.Count == 0)
            {
                continue;
            }

            //store average power and # of rows for that windspeed
            float binnedPower = (float)(binRows.Average(row => row.PowerOutput));
            binnedTelemetry[i] = binnedPower;
            binnedFrequency[i] = binRows.Count;
        }

        return (binnedTelemetry, binnedFrequency);
    }

}
