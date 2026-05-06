namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.MachineLearning.Performance;
using Accord.Math;
using Accord.Statistics.Models.Regression.Linear;
using Accord.Math.Optimization.Losses;
using System.Runtime.CompilerServices;
using Accord.IO;
using Microsoft.VisualBasic;

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


        bool doNormalize = false;
        double[][] input = [];
        double[] output = [];

        if (doNormalize)
        {   
            //normalize input and outputs to 0-1
            double inputMin = inputRaw.Min();
            double inputNormalizationFactor = inputRaw.Max() - inputMin;
            double[] normInput = inputRaw.Select(d => (d-inputMin)/inputNormalizationFactor).ToArray();

            double outputMin = outputRaw.Min();
            double outputNormalizationFactor = outputRaw.Max() - outputMin;
            output = outputRaw.Select(d => (d-outputMin)/outputNormalizationFactor).ToArray();

            input = normInput.Select(value => new double[] { value }).ToArray();
        } else
        {
            input = inputRaw.Select(value => new double[] { value }).ToArray();
            output = outputRaw;
        }
        



        //TODO testtrainsplit this randomly instead of fixed
        int testLength = input.Length / 3;
        int trainLength = input.Length - testLength;
        var (trainInput, trainOutput, testInput, testOutput) = TrainTestSplit(input, output);






        //var svrTrained = DoGridSearch(trainInput, trainOutput);






        

        var svr = new FanChenLinSupportVectorRegression<Gaussian>();
            //according to paper, C and epsilon values are iqr(Y)/13.49
            svr.UseComplexityHeuristic = true;
            //svr.Complexity = 1.0; // what C and other values? omit to make it guess
            svr.UseKernelEstimation = true;
            svr.Kernel = new Gaussian(); //what gamma value?
            svr.Epsilon = 0.01; //default 0.001
            svr.Tolerance = 0.1; //default 0.01

        
        //training happens here
        logger.LogInformation("now training model...");
        SupportVectorMachine<Gaussian> svrTrained = svr.Learn(trainInput, trainOutput);
        logger.LogInformation("Making predictions...");
        double[] scores = svrTrained.Score(testInput);
        
        var topTen = scores.Take(10);
        var topTenTest = testOutput.Take(10);
        logger.LogInformation("SVR RESULTS HERE: {topTen}", topTen);
        logger.LogInformation("ACTUAL VALUES HERE: {topTen}", topTenTest);


        double[] unlistedTestInput = testInput.Select(x => x[0]).ToArray();
        SaveAsCsv(unlistedTestInput, testOutput, scores);

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

        File.WriteAllLines($"TestCSVs/Transform-r2.5.csv", lines);
        counter++;
    }

private void TEMPSaveAsCsv(double[][] input, double[] output, string name)
    {
        double[] normalInput = input.Select(x => x[0]).ToArray();

        var lines = new List<string>{$"PitchAngle,Power"};
        for (int i = 0; i < input.Length; i++)
        {
            lines.Add($"{normalInput[i]},{output[i]}");
        }

        File.WriteAllLines($"TestCSVs/{name}.csv", lines);
        counter++;
    }

private SupportVectorMachine<Gaussian> DoGridSearch(double[][] input, double[] output)
    {
        logger.LogInformation("Starting grid search...");
        var gridsearch = GridSearch<double[], double>.Create(
            ranges: new
            {
                Complexity = GridSearch.Values( 0.01, 0.1, 1, 10, 100 ),
                Gamma = GridSearch.Values(0.001, 0.01, 0.1, 0.5, 1, 10 )
            },

            learner: (p) => new FanChenLinSupportVectorRegression<Gaussian>
            {
                Complexity = p.Complexity,
                Kernel = Gaussian.FromGamma(p.Gamma),
                Epsilon = 0.001, //default 0.001
                Tolerance = 0.01, //default 0.01
                
            },

            fit: (teacher, x, y, w) => teacher.Learn(x, y, w),

            loss: (actual, expected, m) => new SquareLoss(expected).Loss(actual)            
            

        );

        gridsearch.ParallelOptions.MaxDegreeOfParallelism = 4;

        var result = gridsearch.Learn(input, output);
        logger.LogInformation("Grid search Finished");

        var bestSvm = result.BestModel;
        logger.LogInformation("Best parameters - Compexity:{c} Gamma:{g}", result.BestParameters.Complexity.Value, result.BestParameters.Gamma.Value);
        //bestSvm.Save("./");
        return bestSvm;
    }
}