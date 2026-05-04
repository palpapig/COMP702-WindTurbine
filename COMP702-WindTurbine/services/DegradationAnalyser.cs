namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.MachineLearning.Performance;
using Accord.Math;

// ###### TODO ######
// Have a single function which you call to do the analysis. If a trained SVR model does not exist, do the training function (which includes getting 1st year of that turbine's data)
// calculate IQR for Gamma (and complexity?)
// try grid search

public sealed class DegradationAnalyser (
    ILogger<MonitoringWorker> logger,
    int counter = 0
)
{

    public void PerformBenchmark(ICollection<TurbineTelemetry> telemetry, Turbine turbine)
    {
        //get SVR filepath from turbine (make sure when turbine is gotten from supabase, svr filepath is included)

        //test data against the SVR model to get residuals.
        //use averaging equation (in reserach doc) to get single deviation value
        //subtract it from expected deviation

        //save results to supabase
    }

    public void TrainBenchmark(ICollection<TurbineTelemetry> unfilteredTelemetry, Turbine turbine)
    {
        if (turbine.TurbineModel is null)
        {
            logger.LogError("Attempted to benchmark a turbine with no assigned TurbineModel");
            throw new ArgumentNullException(nameof(turbine.TurbineModel));
        }

        ICollection<TurbineTelemetry> telemetry = Benchmarker.Preprocess(unfilteredTelemetry);

        //split into regions 2 and 2.5
        var (region2, region2point5) = SplitRegion(telemetry, turbine.TurbineModel);

        //TrainModel(region2, true);
        TrainModel(region2point5, false);

    }

    static private (ICollection<TurbineTelemetry>, ICollection<TurbineTelemetry>) SplitRegion(ICollection<TurbineTelemetry> telemetry, TurbineModel turbineModel)
    {
        ICollection<TurbineTelemetry> region2 = [];
        ICollection<TurbineTelemetry> region2point5 = [];
        foreach (var row in telemetry)
        {
            var ws = row.CorrectedWindSpeed;
            if ( turbineModel.CutInWindSpeed <= ws && ws < turbineModel.SaturationWindSpeed)
            {
                region2.Add(row);
            } else if ( turbineModel.SaturationWindSpeed <= ws && ws < turbineModel.RatedWindSpeed)
            {
                region2point5.Add(row);
            }
        }

        return (region2, region2point5);
    }

    private void TrainModel(ICollection<TurbineTelemetry> dataset, bool isRegion2)
    {
        // ###### TODO
        // ###### TODO When having model training happen, make sure it awaits/threads properly so it doesn't hold up the whole windows service worker loop
        // ###### TODO

        double[] inputRaw = [];
        double[] outputRaw = dataset.Select(d => d.PowerOutput).ToArray();
        if (isRegion2)
        {
            inputRaw = dataset.Select(d => d.GeneratorSpeed).ToArray();
        } else
        {
            inputRaw = dataset.Select(d => d.PitchAngle).ToArray();
        }


        //normalize input and outputs to 0-1
        double inputMin = inputRaw.Min();
        double inputNormalizationFactor = inputRaw.Max() - inputMin;
        double[] normInput = inputRaw.Select(d => (d-inputMin)/inputNormalizationFactor).ToArray();

        double outputMin = outputRaw.Min();
        double outputNormalizationFactor = outputRaw.Max() - outputMin;
        double[] output = outputRaw.Select(d => (d-outputMin)/outputNormalizationFactor).ToArray();

        double[][] input = normInput.Select(value => new double[] { value }).ToArray();



        //TODO testtrainsplit this randomly instead of fixed
        int testLength = input.Length / 3;
        int trainLength = input.Length - testLength;
        var (trainInput, trainOutput, testInput, testOutput) = TrainTestSplit(input, output);




        var svr = new FanChenLinSupportVectorRegression<Gaussian>
        {
            //according to paper, C and Gamma values are iqr(Y)/13.49
            Complexity = 1.0, // what C and other values? omit to make it guess
            Kernel = new Gaussian(), //what gamma value?
            Epsilon = 0.001, //default 0.001
            Tolerance = 0.01 //default 0.01
        };

        //training happens here
        logger.LogInformation("now training model...");
        var svrTrained = svr.Learn(trainInput, trainOutput);

        double[] scores = svrTrained.Score(testInput);

        var topThirty = scores.Take(30);
        var topThirtyTest = testOutput.Take(30);

        logger.LogInformation("SVR RESULTS HERE: {topThirty}", topThirty);
        logger.LogInformation("ACTUAL VALUES HERE: {topThirty}", topThirtyTest);


        double[] normalTestInput = testInput.Select(x => x[0]).ToArray();
        SaveAsCsv(normalTestInput, testOutput, scores);

        //test SVR on test data to get residuals
        //use averaging equation to get expected deviation

        //serialize and save trained SVR to file

        //upload SVR filepath and expected deviation to supabase
    }

    private (double[][], double[], double[][], double[]) TrainTestSplit(double[][] input, double[] output)
    {
        int trainLength = (int)(input.Length * 0.66);
        double[][] trainInput = input.Take(trainLength).ToArray();
        double[] trainOutput = output.Take(trainLength).ToArray();
        double[][] testInput = input.Skip(trainLength).ToArray();
        double[] testOutput = output.Skip(trainLength).ToArray();

        return (trainInput, trainOutput, testInput, testOutput);
    }



private void SaveAsCsv(double[] input, double[] output, double[] scores)
    {
        var lines = new List<string>{$"GeneratorSpeed or PitchAngle,Power,PredictedPower"};
        for (int i = 0; i < input.Length; i++)
        {
            lines.Add($"{input[i]},{output[i]},{scores[i]}");
        }

        File.WriteAllLines($"TestCSVs/normalized2.5.csv", lines);
        counter++;
    }



}