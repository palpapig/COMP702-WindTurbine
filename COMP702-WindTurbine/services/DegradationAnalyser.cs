namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Statistics.Kernels;
using Accord.MachineLearning.Performance;

// ###### TODO ######
// Put region 2 and 2.5 splitting into its own function.
// Have a single function which you call to do the analysis. If a trained SVR model does not exist, do the training function (which includes getting 1st year of that turbine's data)

public sealed class DegradationAnalyser (
    ILogger<MonitoringWorker> logger
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
        TurbineModel turbineModel = turbine.TurbineModel;

        ICollection<TurbineTelemetry> telemetry = Benchmarker.Preprocess(unfilteredTelemetry);

        //split into regions 2 and 2.5
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

        TrainModel(region2);
        TrainModel(region2point5);

    }

    private void TrainModel(ICollection<TurbineTelemetry> dataset)
    {
        // ###### TODO
        // ###### TODO When having model training happen, make sure it awaits/threads properly so it doesn't hold up the whole windows service worker loop
        // ###### TODO

        //TrainTestSplit

        //train SVR on train data

        //test SVR on test data to get residuals
        //use averaging equation to get expected deviation

        //serialize and save trained SVR to file

        //upload SVR filepath and expected deviation to supabase
    }



}