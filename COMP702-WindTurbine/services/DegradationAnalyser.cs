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
    ILogger<MonitoringWorker> logger
    )
{

    public void PerformBenchmark(ICollection<TurbineTelemetry> telemetry, Turbine turbine)
    {
        if (turbine.DegradationModelDetails == null)
        {
            logger.LogWarning("Degradation Model not found for turbine {t}. Training degradation model now...", turbine.TurbineId);
            TrainBenchmark(telemetry, turbine);
            //Somehow re-get from supabase.
            return;
        }
        var modelPath = turbine.DegradationModelDetails.Filepath;
        var expectedDeviation = turbine.DegradationModelDetails.Offset;

        //get model file
        //pass file and expectedDeviation to python: (if ONNX, this can all be done in C#)

        //test data against the SVR model to get residuals. 
        //use averaging equation (in reserach doc) to get deviation value
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
        CreateModel(region2point5, false);
        logger.LogInformation("Degradation Benchmark Models successfuly created for turbine {t}", turbine.TurbineId);

    }

   

    private void CreateModel(ICollection<TurbineTelemetry> dataset, bool isRegion2)
    {
        //do a train-test split

        //pass training data to python
        //this is what python does with it:

            //normalize training data?

            //do grid search for best hyperprams using training data
                //either have them be tightly bunched assumed-correct params,
                //OR, do a double grid search where you take the best params and do a tighter search around that. 


            //validation, not for production: also have the testing data passed and do this:
                //also test data to get RMSE, R2, MAE values
                //put test/actual data into grapher


        //python returns serialized model
            //serialized using ONNX?

        //use test data to get residuals; use averaging equation to get expected-deviation

        //save SVR to file
        //upload SVR filename and expected deviation to supabase
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
}