using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using COMP702_WindTurbine.models;
//using System.Text.Json.Serialization;

public sealed class FailureDetection2 : IDisposable
{
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


    public FailureDetection2(string onnxModelPath)
    {
        _session = new InferenceSession(onnxModelPath);


        inputName = _session.InputMetadata.Keys.First();
    }

    public float Predict(RawData rawdata, TurbineTelemetry telemetry, CancellationToken ct)
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


    public void Dispose()
    {
        _session.Dispose();
    }
}