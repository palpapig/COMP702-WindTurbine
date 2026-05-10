using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using COMP702_WindTurbine.models;
using COMP702_WindTurbine.services;




namespace COMP702_WindTurbine.services;

public class FailureDetectionResult
{
    public string TurbineId { get; set; } = "";

    //  public TurbineTelemetry Telemetry { get; set; } = new();

    public DateTime? Timestamp { get; set; }

    public double? Residual { get; set; }

    public bool IsAbnormal { get; set; }

    public int AlarmLvl { get; set; }

    public double? PredictedValue { get; set; }

    public double? ActualValue { get; set; }

    public double? LCL { get; set; }
    public double? UCL { get; set; }
    public double EWMA { get; set; }
    public bool A1Triggered { get; set; }
    public bool A2Triggered { get; set; }


    public Alarm? Alarm { get; set; }
}
public class FailureDetection2 : IDisposable
{
    private readonly FailureDetectionAlarm _alarmService;
    private readonly InferenceSession _session;

    private readonly string inputName;

    private readonly string[] featureColumns =
  {
        "RotorSpeed",
        "GearOilInletTemp",
        "GeneratorBearingFrontTemp",
        "RearBearingTemp",
        "GearOilInletPressure",
        "NacelleTemp"
    };


    public FailureDetection2(string onnxModelPath, FailureDetectionAlarm alarmService)
    {
        _session = new InferenceSession(onnxModelPath);

        _alarmService = alarmService;

        inputName = _session.InputMetadata.Keys.First();
    }

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



        var inputTensor = new DenseTensor<float>(inputValues, new[] { 1, featureColumns.Length });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
        };

        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = _session.Run(inputs);

        float prediction = results.First().AsEnumerable<float>().First();

        return prediction;
    }
    public FailureDetectionResult DetectFailure(RawData rawdata, TurbineTelemetry telemetry, CancellationToken ct)
    {

        float predictedValue = Predict(rawdata, ct);

        double actualValue = rawdata.GearboxOilTemp;
        double residual = actualValue - predictedValue;

        Alarm alarm = _alarmService.Evaluate(
            telemetry.TurbineId,
            residual
        );

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

        return new FailureDetectionResult
        {

            TurbineId = telemetry.TurbineId,
            Timestamp = telemetry.Timestamp,
            PredictedValue = predictedValue,
            ActualValue = actualValue,
            Residual = residual,
            IsAbnormal = alarmLevel > 0,
            AlarmLvl = alarmLevel,
            UCL = alarm.UCL,
            LCL = alarm.LCL,
            EWMA = alarm.EWMA,
            A1Triggered = alarm.A1Triggered,
            A2Triggered = alarm.A2Triggered,

            Alarm = alarm
        };
    }


    public void Dispose()
    {
        _session.Dispose();
    }
}