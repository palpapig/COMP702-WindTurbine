namespace COMP702_WindTurbine.services;
using System.Text.Json;
using COMP702_WindTurbine.models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Npgsql.Replication;
using System.Diagnostics;

// ###### TODO ######
// Have a single function which you call to do the analysis. If a trained SVR model does not exist, do the training function (which includes getting 1st year of that turbine's data)
// calculate IQR for Gamma (and complexity?)
// try grid search

public sealed class DegradationAnalyser (
    ILogger<MonitoringWorker> logger,
    IServiceScopeFactory scopeFactory)
{

    public void Run(ICollection<TurbineTelemetry> telemetry, Turbine turbine)
    {
        if (turbine.TurbineModel is null)
        {
            logger.LogError("Attempted to benchmark a turbine with no assigned TurbineModel");
            throw new ArgumentNullException(nameof(turbine.TurbineModel));
        }
        var turbineModel = turbine.TurbineModel;

        //preprocess data (remove out-of-range values) for both training and testing
        telemetry = Benchmarker.Preprocess(telemetry);


        //Split data into inputs and outputs for regions 2 and 2.5 (region2point5)
        //Do not include rows with either missing value
        List<double> tempInputRegion2 = [];
        List<double> tempOutputRegion2 = [];
        List<double> tempInputRegion2p5 = [];
        List<double> tempOutputRegion2p5 = [];
        foreach (var row in telemetry)
        {
            var ws = row.CorrectedWindSpeed;
            if ( turbineModel.CutInWindSpeed <= ws && ws < turbineModel.SaturationWindSpeed)
            {
                if(!(double.IsNaN(row.GeneratorSpeed) || double.IsNaN(row.PowerOutput))) //dont add if either value is NaN
                {
                    tempInputRegion2.Add(row.GeneratorSpeed);
                    tempOutputRegion2.Add(row.PowerOutput);
                }
            } else if ( turbineModel.SaturationWindSpeed <= ws && ws < turbineModel.RatedWindSpeed)
            {
                if(!(double.IsNaN(row.PitchAngle) || double.IsNaN(row.PowerOutput))) //dont add if either value is NaN
                {
                    tempInputRegion2p5.Add(row.PitchAngle);
                    tempOutputRegion2p5.Add(row.PowerOutput);
                }
            }
        }
        var inputRegion2 = tempInputRegion2.ToArray();
        var outputRegion2 = tempOutputRegion2.ToArray();
        var inputRegion2p5 = tempInputRegion2p5.ToArray();
        var outputRegion2p5 = tempOutputRegion2p5.ToArray();


        //TODO two DegModels should be stored per turbine, one for each region.
        //
        if (turbine.DegradationModelDetails == null)
        {
            logger.LogWarning("Degradation Model not found for turbine {t}. Training degradation model now...", turbine.TurbineId);
            PythonTrainBenchmark(inputRegion2, outputRegion2, turbine);
            PythonTrainBenchmark(inputRegion2p5, outputRegion2p5, turbine);
            //TODO Somehow re-get from supabase. or just pass back the TurbineModelDetails. Also check the written file exists.
            return;
        }

        //if the turbine already has a trained model, use it do to the benchmark
        PerformBenchmark(inputRegion2, outputRegion2, turbine);
        PerformBenchmark(inputRegion2p5, outputRegion2p5, turbine);
    }

    private void PerformBenchmark(double[] inputData, double[] actualOutput, Turbine turbine)
    {
        
        var expectedDeviation = turbine.DegradationModelDetails?.Offset;
        var modelName = turbine.DegradationModelDetails?.Filepath; //TODO this is copied code from the other function
        var modelPath = $"PythonDegradationTraining/Models/{modelName}";

        //check ONNX file exists
        if (!File.Exists(modelPath))
        {
            logger.LogError("Attempted to access degradation model .onnx file which does not exist");
            throw new ArgumentNullException(nameof(turbine.TurbineModel));
        }

        //read ONNX model from file and set it up with the input data
        var session = new InferenceSession(modelPath);   
        long[] dimensions = [inputData.Length, 1];
        using var inputOrtValue = OrtValue.CreateTensorValueFromMemory(inputData, dimensions);

        var inputs = new Dictionary<string, OrtValue>
        {
            {"inputVar", inputOrtValue }
        };

        using var runOptions = new RunOptions();


        //get power predictions
        using var output = session.Run(runOptions, inputs, [session.OutputNames[0]]);

        var output_0 = output[0];

        var outputData = output_0.GetTensorDataAsSpan<double>();
        var tensorTypeAndShape = output_0.GetTensorTypeAndShape();
        logger.LogCritical("ONNX tensor first output: {o}", outputData[0]);
        logger.LogCritical("ONNX tensor shape: {s}", tensorTypeAndShape);
        
        //TODO VVVV
        //test data against the SVR model to get residuals. 

        //use averaging equation (in reserach doc) to get deviation value
        //subtract it from expected deviation

        //save results to supabase

        //QUALITATIVE BONUS: do manufacturer model benchmarking, but again against itself.
            //bin the data into 0.5 generatorspeed or blade pitch.
            //get standard deviations of each bin
            //store that in supabase somehow
    
    }

    private void PythonTrainBenchmark(double[] input, double[] output, Turbine turbine)
    {
        var modelName = turbine.DegradationModelDetails?.Filepath; //TODO this is copied code from the other function
        var modelPath = $"PythonDegradationTraining/Models/{modelName}";

        //write input and output rows to file for python to train with
        var dataPath = "PythonDegradationTraining/Models/TempTrainingData.csv";
        var lines = new List<string>();

        lines.Add($"inputVar,power");
        for (int i = 0; i < input.Length; i++)
        {
            lines.Add($"{input[i]},{output[i]}");
        }

        File.WriteAllLines(dataPath, lines);


        //Setting up to run the python script for model training...
        var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"DegradationTraining.py {dataPath} {modelPath}", //provide the .py filename and training data filename 

                RedirectStandardOutput = true,
                RedirectStandardError = true,

                UseShellExecute = false,
                CreateNoWindow = true
            };
        
        //Running model training script.
        using var process = new Process();
        process.StartInfo = startInfo;
        process.Start();

        string pyOutput = process.StandardOutput.ReadToEnd();
        string pyError = process.StandardError.ReadToEnd();

        process.WaitForExit();

        //logging response from python
        if (!string.IsNullOrWhiteSpace(pyError))
        {
            logger.LogError("Degradation model training script Encountered an Error: {o}", pyError);
        } 
        PyResponse? response = JsonSerializer.Deserialize<PyResponse>(pyOutput);
        logger.LogInformation("Degradation model training script exited with message: {o}", response?.Message);
        
        
        //save the details of the newly trained model to supabase
        using (var tempScope = scopeFactory.CreateScope())
        {
            DegradationModelDetails degradationModelDetails = new()
            {
                Offset = (float)response?.ExpectedDeviation,
                Filepath = modelPath,
                Turbine = turbine
            };

            var dbService = tempScope.ServiceProvider.GetRequiredService<DbService>();
            
        }


        logger.LogInformation("Degradation Benchmark Models successfuly created for turbine {t}", turbine.TurbineId);



    }

    class PyResponse()
    {
        public bool Success { get; set; } = false;
        public double? ExpectedDeviation { get; set; }
        public string? Message { get; set; }
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