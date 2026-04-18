namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;


public sealed class Benchmarker
{
    public TurbineTelemetry DummyBenchmark(TurbineTelemetry telemetry)
    {
        var rand = new Random();
        telemetry.Efficiency = Math.Round(rand.NextDouble()*100, 2);
        return telemetry;
    }

    public void Benchmark(ICollection<TurbineTelemetry> telemetry, TurbineModel turbineModel)
    {
        telemetry = Preprocess(telemetry);
        Dictionary<float, float> actualBins = BinTelemetry(telemetry, turbineModel.CutInWindSpeed, turbineModel.CutOutWindSpeed); 


        ICollection<PowerCurveBin> expectedBins = turbineModel.PowerCurveBins;
        ICollection<PowerCurveBin> deviationBins = [];

        foreach (PowerCurveBin expectedBin in expectedBins)
        {   
            try
            {
                float bin = expectedBin.WindSpeedMs;
                float actualPower = actualBins[bin]; 
                float deviation = actualPower - expectedBin.PowerKw;

                deviationBins.Add(new PowerCurveBin
                {
                    WindSpeedMs = bin,
                    PowerKw = deviation
                });
            } catch
            {
                
            }
        }

        BenchmarkResult result = new BenchmarkResult
        {
            DeviationScore = 0,
            DeviationBins = deviationBins,
            PowerBins = 0,
            
            
        }



        //PowerCurveBin expectedBins = turbineModel.PowerCurveBins.First(bin => bin.WindSpeedMs == 0.5f);
        //float expectedPower = expectedBin.PowerKw;

    }

    private ICollection<TurbineTelemetry> Preprocess(ICollection<TurbineTelemetry> telemetry, bool hasCorrectedWindSpeed = true)
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

    private Dictionary<float, float> BinTelemetry(ICollection<TurbineTelemetry> telemetry, float minBin, float maxBin, float binInterval = 0.5f)
    {


        Dictionary<float, float> binnedTelemetry = new();
        

        for (float i = minBin; i <= maxBin; i += binInterval)
        {   
            float lowerBound = i - (binInterval/2);
            float upperBound = i + (binInterval/2);
            
            //filter for rows in bin
            ICollection<TurbineTelemetry> binRows = [.. telemetry.Where(row => lowerBound < row.CorrectedWindSpeed && row.CorrectedWindSpeed < upperBound )];

            float binnedPower = (float) binRows.Average(row => row.PowerOutput);
            binnedTelemetry[i] = binnedPower;
        }

        return binnedTelemetry;
    }

}