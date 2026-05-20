namespace COMP702_WindTurbine.services;
using System.Text.Json;
using COMP702_WindTurbine.models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Npgsql.Replication;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

public sealed class DegradationAnalyser (
    ILogger<MonitoringWorker> logger,
    IServiceScopeFactory scopeFactory,
    PlaceholderHistoricalDataSource historicalDataSource)
{
    readonly string modelsDirectory = "PythonDegradationTraining/Models/";

    /// <para>Performs degradation analysis on the turbine with the given Id and the described time period.
    /// </para>
    /// Turbine information is read directly from Supabase.
    /// Telemetry data is read from PlaceholderHistoricalDataSource.cs, which reads from the same .csv as the live data simulator.
    /// </summary>
    public async Task ForceDoAnalysis(DateTime endDate, bool forceRetrain = false, string turbineId = "BK-TEST-4",  int monthsGap = 12){
        using (var tempScope = scopeFactory.CreateScope())
            {
                var dbService = tempScope.ServiceProvider.GetRequiredService<DbService>();

                Turbine turbine = await dbService.GetTurbineById(turbineId);
                
                //Get telemetry to be analysed
                List<TurbineTelemetry> analysisTelemetry = historicalDataSource.GetHistoricalTurbineData(monthsGap, endDate);
                
                //The actual degradation analysis happens in this function here
                DegradationResult degradationResult = await DoDegradationAnalysis(analysisTelemetry, turbine, forceRetrain);
                if ( degradationResult != null)
                {
                    await dbService.AddDegradationResult(degradationResult);
                    logger.LogInformation("Degradation result successfuly added to database!");
                }

            }
    }

    /// <summary>
    /// <para> Checks if the turbine with the given turbine Id has a DegradationResult ending in the last monthsGap months. If not, performs analysis on the last monthsGap months of telemetry starting at currentDate
    /// </para>
    /// Turbine information is read directly from Supabase.
    /// Telemetry data is read from PlaceholderHistoricalDataSource.cs, which reads from the same .csv as the live data simulator.
    /// </summary>
    public async Task DoAnalysisIfNeeded(string turbineId, DateTime currentDate, int monthsGap = 12){
        DateTime endDate = currentDate;
        using (var tempScope = scopeFactory.CreateScope())
            {
                var dbService = tempScope.ServiceProvider.GetRequiredService<DbService>();
                
                Turbine turbine = await dbService.GetTurbineById(turbineId);

                //if there is at least one degradation result, AND the most recent one happened less than monthGap months ago, don't do analysis
                if (turbine.DegradationResults.Count > 0)
                {
                    DateTime lastAnalysed = turbine.DegradationResults.MaxBy(r => r.TimeRangeEnd).TimeRangeEnd;
                    if (lastAnalysed.AddMonths(monthsGap) > endDate)
                    {
                        logger.LogInformation("No degradation analysis needed, latest analysis happened less than {m} months ago", monthsGap);
                        return;
                    }
                }
                
                //Get telemetry to be analysed
                List<TurbineTelemetry> recentTelemetry = historicalDataSource.GetHistoricalTurbineData(monthsGap, endDate);

                if (recentTelemetry.Count == 0)
                {
                    logger.LogWarning("Attempted to do degradation analysis on turbine {t}, but no data exists for given time period", turbine.TurbineId);
                    return;
                }
                
                //The actual degradation analysis happens in this function here
                DegradationResult degradationResult = await DoDegradationAnalysis(recentTelemetry, turbine);
                if (degradationResult != null)
                {
                    await dbService.AddDegradationResult(degradationResult);
                    logger.LogInformation("Degradation result successfuly added to database!");
                }

            }
    }

    /// <summary>
    /// <para>The core function which actually performs degradation analysis.
    /// Provided Telemetry is assumed to be owned by the Turbine. 
    /// </para>
    /// <para>Splits telemetry into regions 2 and 2.5 (2p5) based on wind speed. Performs analysis on each region using the trained degradation models associated with the given turbine.
    /// </para>
    /// <para>If trained degradation models for the turbine don't yet exist, the first year of turbine telemetry will be read from PlaceholderHistoricalDataSource.cs and sent to a python script to train SVR models on.
    /// One model is trained for each region.
    /// The models are stored as .onnx files and their file paths are stored in a DegradationModelDetails entry and saved to the database.
    /// </para>
    /// <para>Uses the trained model to get the difference between actual and predicted power for every row of telemetry.
    /// Averages those residuals into a final degradation score for that time period and returns a DegradationResult with that information.
    /// </para>
    /// </summary>
    public async Task<DegradationResult> DoDegradationAnalysis(ICollection<TurbineTelemetry> telemetry, Turbine turbine, bool forceRetrain = false)
    {
        //check that the provided turbine has an associated turbineModel
        if (turbine.TurbineModel is null)
        {
            logger.LogError("Attempted to benchmark a turbine with no assigned TurbineModel");
            return null;
        }
        var turbineModel = turbine.TurbineModel;


        //preprocess data (remove out-of-range values)
        telemetry = Benchmarker.Preprocess(telemetry);

        //Split data into inputs and outputs for regions 2 and 2.5 (region2point5)
        //Do not include rows with either missing value
        var (inputRegion2, outputRegion2,  inputRegion2p5, outputRegion2p5) = SplitRegions(telemetry, turbineModel);
        
        DegradationModelDetails degDetails = turbine.DegradationModelDetails;

        //check if turbine has degradationModelDetails exists for this turbine, and if matching model files exist
        bool trainingRequired = false;
        if (degDetails == null || forceRetrain){
            trainingRequired = true;
            logger.LogInformation("DegradationModelDetails not found for turbine {t}. Training degradation model now...", turbine.TurbineId);

        } else {
            string path2 = GenerateModelPath(degDetails.Region2Filename);
            string path2p5 = GenerateModelPath(degDetails.Region2p5Filename);

            if(!File.Exists(path2) || !File.Exists(path2p5)){
                trainingRequired = true;
                logger.LogInformation("DegradationModelDetails found for turbine {t}, but model files do not exist. Training degradation model now...", turbine.TurbineId);

            }
        }

        //if model files aren't found, create and train a new model
        if (trainingRequired){
            degDetails = await TrainModels(turbine);
            logger.LogInformation("Degradation Model created for turbine {t}. Doing degradation analysis now...", turbine.TurbineId);
        } else {
            logger.LogInformation("Degradation Model found for turbine {t}. Doing degradation analysis now...", turbine.TurbineId);
        }



        //for each region, use the trained model to get a benchmark deviation score
        string modelPathRegion2 = GenerateModelPath(degDetails.Region2Filename);
        string modelPathRegion2p5 = GenerateModelPath(degDetails.Region2p5Filename);

        float region2Deviation = AnalyseRegion(inputRegion2, outputRegion2, turbine.DegradationModelDetails, modelPathRegion2, "2");
        float region2p5Deviation = AnalyseRegion(inputRegion2p5, outputRegion2p5, turbine.DegradationModelDetails, modelPathRegion2p5, "2p5");

        //combine results from each region
        DegradationResult degradationResult = new() {
            Region2Score = region2Deviation,
            Region2Point5Score = region2p5Deviation,
            TimeRangeStart = telemetry.MinBy(t => t.Timestamp).Timestamp,
            TimeRangeEnd = telemetry.MaxBy(t => t.Timestamp).Timestamp,
            TurbineId = turbine.TurbineId
        };

        return degradationResult;
    }

    private float AnalyseRegion(float[] inputData, float[] actualOutput, DegradationModelDetails degModelDetails, string modelPath, string region)
    {
        string inputVarName;
        float expectedDeviation;
        if (region == "2"){
            expectedDeviation = degModelDetails.Region2Offset;
            inputVarName = "GeneratorSpeed";
        } else if (region == "2p5") {
            expectedDeviation = degModelDetails.Region2p5Offset;
            inputVarName = "PitchAngle";
        } else
        {
            logger.LogCritical("Invalid region number provided to benchmarking function");
            return 0;
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

        var outputData = output_0.GetTensorDataAsSpan<float>();
        var tensorTypeAndShape = output_0.GetTensorTypeAndShape();
        //logger.LogInformation("ONNX tensor first output: {o}", outputData[0]);

        //write results to csv
        var dataPath = $"PythonDegradationTraining/outputs/csharp_{degModelDetails.TurbineId}region-{region}.csv";
        var lines = new List<string>();

        lines.Add($"{inputVarName},Power,PredictedPower");
        for (int i = 0; i < inputData.Length; i++)
        {
            lines.Add($"{inputData[i]},{actualOutput[i]},{outputData[i]}");
        }

        File.WriteAllLines(dataPath, lines);

        
        //get residuals from pred - actual
        var residuals = new float[actualOutput.Length];
        for (int i = 0; i < actualOutput.Length; i++)
        {
            residuals[i] = actualOutput[i] - outputData[i];
        }

        float deviation = residuals.Sum() / actualOutput.Sum();
        float deviationPercentage = deviation * 100;

        float correctedDeviation = deviationPercentage - expectedDeviation; //expectedDeviation was already stored as a percentage

        return correctedDeviation;
    
    }

    /// <summary>
    /// <para>Reads the first year of a turbine's telemetry and uses it to train an SVR model for each region, then save them as .onnx files.
    /// Saves a DegredationModelDetails entry related to the turbine in the database
    /// </para>
    /// 
    /// </summary>
    private async Task<DegradationModelDetails> TrainModels(Turbine turbine)
    {   
        using var tempScope = scopeFactory.CreateScope();   
        var dbService = tempScope.ServiceProvider.GetRequiredService<DbService>();

        //get first year of data to train model on (always gets turbine BK-TEST-4)
        ICollection<TurbineTelemetry> telemetry = historicalDataSource.GetEarliestTurbineData();
        
        //preprocess data (remove out-of-range values)
        telemetry = Benchmarker.Preprocess(telemetry);

        //Split data into inputs and outputs for regions 2 and 2.5 (region2point5)
        //Does not include rows where either value is missing
        var (inputRegion2, outputRegion2, inputRegion2p5, outputRegion2p5) = SplitRegions(telemetry, turbine.TurbineModel);

        string modelNameRegion2 = GenerateModelName(turbine.TurbineId, "2");
        string modelNameRegion2p5 = GenerateModelName(turbine.TurbineId, "2p5");

        //train model for each region and save it to disk, returning details of each
        float deviationRegion2 = TrainRegion(inputRegion2, outputRegion2, turbine, modelNameRegion2);
        float deviationRegion2p5 = TrainRegion(inputRegion2p5, outputRegion2p5, turbine, modelNameRegion2p5);

        DegradationModelDetails degradationModelDetails = new() {
            TurbineId = turbine.TurbineId,
            Region2Offset = deviationRegion2,
            Region2Filename = modelNameRegion2,
            Region2p5Offset = deviationRegion2p5,
            Region2p5Filename = modelNameRegion2p5,
        };

        logger.LogInformation("Adding trained model details to database...");
        //write model details to database
        await dbService.AddDegradationModelDetails(degradationModelDetails);
        return degradationModelDetails;
    }

    ///<summary>
    /// <para>Trains one SVR model using one region of telemetry. Telemetry is stored as a .csv then a python script runs and reads that .csv file.
    /// It runs a grid search for optimal hyperparameters then saves the most effective model as an .onnx file.
    /// </para>
    /// 
    /// </summary>
    private float TrainRegion(float[] input, float[] output, Turbine turbine, string modelName){
        //create file name and path for .onnx model to be created
        string modelPath = GenerateModelPath(modelName);

        //write input and output training rows to csv for python to train with
        var dataPath = "PythonDegradationTraining/TempTrainingData.csv";
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
                Arguments = $"PythonDegradationTraining/DegradationTraining.py {dataPath} {modelPath} {modelName}", //provide the .py filename, training data path, and model path/name 

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
        
        logger.LogInformation("Degradation Benchmark Models successfuly created for turbine {t}", modelName);
        return (float)response?.ExpectedDeviationPercentage;
    }

    class PyResponse()
    {
        public bool Success { get; set; } = false;
        public float? ExpectedDeviationPercentage { get; set; }
        public string? Message { get; set; }
    }

    /// <summary>
    /// Helper function to split telemetry into regions based on wind speed and extract the relevant variables for each region.
    /// Generator speed and power for region 2, Blade pitch and power for region 2.5.
    /// </summary>

    static private (float[], float[], float[], float[]) SplitRegions(ICollection<TurbineTelemetry> telemetry, TurbineModel turbineModel)
    {
        List<float> tempInputRegion2 = [];
        List<float> tempOutputRegion2 = [];
        List<float> tempInputRegion2p5 = [];
        List<float> tempOutputRegion2p5 = [];
        foreach (var row in telemetry)
        {
            var ws = row.CorrectedWindSpeed;
            if ( turbineModel.CutInWindSpeed <= ws && ws < turbineModel.SaturationWindSpeed)
            {
                if(!(double.IsNaN(row.GeneratorSpeed) || double.IsNaN(row.PowerOutput))) //dont add if either value is NaN
                {
                    tempInputRegion2.Add((float)row.GeneratorSpeed);
                    tempOutputRegion2.Add((float)row.PowerOutput);
                }
            } else if ( turbineModel.SaturationWindSpeed <= ws && ws < turbineModel.RatedWindSpeed)
            {
                if(!(double.IsNaN(row.PitchAngle) || double.IsNaN(row.PowerOutput))) //dont add if either value is NaN
                {
                    tempInputRegion2p5.Add((float)row.PitchAngle);
                    tempOutputRegion2p5.Add((float)row.PowerOutput);
                }
            }
        }
        float[] inputR2 = tempInputRegion2.ToArray();
        float[] outputR2 = tempOutputRegion2.ToArray();
        float[] inputR2p5 = tempInputRegion2p5.ToArray();
        float[] outputR2p5 = tempOutputRegion2p5.ToArray();

        return (inputR2, outputR2, inputR2p5, outputR2p5);
    }

    private string GenerateModelName(string turbineId, string region)
    {
        return $"{turbineId}-region{region}"; 

    }

    private string GenerateModelPath(string modelName)
    {
        return $"{modelsDirectory}{modelName}.onnx"; 

    }
}
