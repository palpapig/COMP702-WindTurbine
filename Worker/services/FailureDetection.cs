using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using COMP702_WindTurbine.models;
using COMP702_WindTurbine.services;


namespace COMP702_WindTurbine.services;
/*
    FailureDetection is responsible for running the trained ONNX model
    and using its prediction to detect possible turbine faults.

    It does three main things:
    1. Takes raw turbine sensor data.
    2. Uses the ONNX model to predict the expected gearbox oil temperature.
    3. Compares the predicted value with the actual value and creates alarm/result data.
*/

public class FailureDetection : IDisposable
{
    private readonly FailureDetectionAlarm _alarmService;
    private readonly InferenceSession _session;

    private readonly string inputName;


    /*
         These are the feature columns used by the model.
         The order here must match the exact order used during model training.
         If this order changes, the prediction can become wrong.
         you can check trianedmodel/metadeta.jason for exact order
     */
    private readonly string[] featureColumns =
  {
        "RotorSpeed",
        "GearOilInletTemp",
        "GeneratorBearingFrontTemp",
        "RearBearingTemp",
        "GearOilInletPressure",
        "NacelleTemp"
    };


    public FailureDetection(string onnxModelPath, FailureDetectionAlarm alarmService)
    {
        _session = new InferenceSession(onnxModelPath);

        _alarmService = alarmService;

        inputName = _session.InputMetadata.Keys.First();
    }

    // Predict:Converts raw turbine values into the format expected by the ONNX model.Runs the ONNX model.Returns the predicted gearbox oil temperature.
    public float Predict(RawData rawdata, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // this has to be the exact same order as it was trianed, features order will be found in training metadata
        float[] inputValues =
        {
            (float)rawdata.RotorSpeed!,
            (float)rawdata.GearOilInletTemp!,
            (float)rawdata.GeneratorBearingFrontTemp!,
            (float)rawdata.RearBearingTemp!,
            (float)rawdata.GearOilInletPressure!,
            (float)rawdata.NacelleTemp!
        };


        // Create a tensor with one row and the number of model features.
        var inputTensor = new DenseTensor<float>(inputValues, new[] { 1, featureColumns.Length });
        // Package the tensor using the input name expected by the ONNX model.
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };


        // Run the ONNX model and get the prediction result.
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

        float prediction = results.First().AsEnumerable<float>().First();

        return prediction;
    }


    /*
      DetectFailure:
      - Gets the model prediction.
      - Compares actual gearbox oil temperature with predicted temperature.
      - Calculates the residual.
      - Sends the residual to the alarm service.
      - Stores the prediction and alarm result inside the telemetry object.
  */
    public TurbineTelemetry DetectFailure(RawData rawdata, TurbineTelemetry telemetry, CancellationToken ct)
    {

        if (double.IsNaN(rawdata.GearboxOilTemp) || rawdata.GearboxOilTemp < -50 || rawdata.GearboxOilTemp > 150)
        {
            throw new InvalidOperationException($"Invalid GearboxOilTemp value: {rawdata.GearboxOilTemp}");
        }

        float predictedValue = Predict(rawdata, ct);

        double actualValue = rawdata.GearboxOilTemp;
        double residual = actualValue - predictedValue;


        //Evaluate the residual using the alarm logic.
        // The turbine ID is used so each turbine keeps its own alarm state.
        Alarm alarm = _alarmService.Evaluate(
            telemetry.TurbineId,
            residual
        );

        // this line set ther right alarm levl, if both alarm 1 and 2 not triggred, default lvl is 1 meaning not faulty

        int alarmLevel;

        if (alarm.A2Triggered)
        {
            alarmLevel = 2;
        }
        else if (alarm.A1Triggered)
        {
            alarmLevel = 1;
        }
        else
        {
            alarmLevel = 0;
        }

        // return telemetry with reuslt value

        telemetry.FailureDetectionResult = new FailureDetectionResult();

        telemetry.FailureDetectionResult.TurbineId = telemetry.TurbineId;
        telemetry.FailureDetectionResult.Timestamp = telemetry.Timestamp;

        telemetry.FailureDetectionResult.ActualValue = telemetry.GearboxOilTemp;
        telemetry.FailureDetectionResult.PredictedValue = predictedValue;
        telemetry.FailureDetectionResult.Residual = residual;
        telemetry.FailureDetectionResult.IsAbnormal = alarmLevel > 0;
        telemetry.FailureDetectionResult.AlarmLvl = alarmLevel;
        telemetry.FailureDetectionResult.UCL = alarm.UCL;
        telemetry.FailureDetectionResult.LCL = alarm.LCL;
        telemetry.FailureDetectionResult.EWMA = alarm.EWMA;
        telemetry.FailureDetectionResult.A1Triggered = alarm.A1Triggered;
        telemetry.FailureDetectionResult.A2Triggered = alarm.A2Triggered;


        return telemetry;


    }


    public void Dispose()
    {
        _session.Dispose();
    }
}