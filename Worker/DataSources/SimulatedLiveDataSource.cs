/*
 this is the simulated live data source for our wind turbine monitoring system
 it implements the idatasource interface so the pipeline can fetch data from it

 built this after analysing 1 year of real scada data from kelmarsh wind farm (turbine 1, 2019)
 the analysis gave realistic parameters for wind speed, power curve, rotor speed etc
 you can see those numbers in appsettings.json under "SimulatedDataSource"

 there are two modes:
   - replay: reads the actual cleaned csv file (turbine1_clean.csv) and cycles through it
             add a tiny bit of random noise so it's not exactly the same every time
   - generative: creates synthetic data on the fly using the statistical models derived from the real data
                 this is good for testing scenarios that aren't in the historical record

 even though only used 1 turbine's data, the source can simulate multiple turbines
 by rotating through a list of turbine ids (turbinecount in config)
 later, can add per‑turbine parameters for realism
 */

using COMP702_WindTurbine.models;
using System.Globalization;

namespace COMP702_WindTurbine.DataSources;

public class SimulatedLiveDataSource : IDataSource
{
    private readonly ILogger<SimulatedLiveDataSource> _logger;
    private readonly SimulatedDataSourceConfig _config;               // holds all the settings from appsettings.json
    private readonly Random _random = new();                           // for all the random numbers we need
    private readonly List<ReplayRecord> _replayData = new();          // stores the csv data in memory for replay mode
    private int _replayIndex = 0;                                      // current position in the replay list
    private int _turbineIndex = 0;                                      // which turbine id we'll give to the next data point
    private readonly List<string> _turbineIds;                         // list of turbine ids like "WT-001", "WT-002" etc

    // for generative mode we pre‑process the noise bins into arrays for quick lookup
    // these are nullable because they're only set in generative mode
    private readonly double[]? _noiseBinCenters;
    private readonly double[]? _noiseBinStds;

    public SimulatedLiveDataSource(ILogger<SimulatedLiveDataSource> logger, IConfiguration configuration)
    {
        _logger = logger;
        // read the config section and fall back to defaults if it's missing
        _config = configuration.GetSection("SimulatedDataSource").Get<SimulatedDataSourceConfig>()
                  ?? new SimulatedDataSourceConfig();

        // build turbine ids based on how many the user wants to simulate
        _turbineIds = Enumerable.Range(1, _config.TurbineCount)
                                 .Select(i => $"WT-{i:D3}")
                                 .ToList();

        // if we're in replay mode, load the csv file
        if (_config.Mode.Equals("Replay", StringComparison.OrdinalIgnoreCase))
        {
            LoadReplayData();
        }
        else if (_config.Mode.Equals("Generative", StringComparison.OrdinalIgnoreCase))
        {
            // extract the noise bins for fast interpolation later
            var bins = _config.Generative.PowerCurve.NoiseBins;
            _noiseBinCenters = bins.Select(b => b[0]).ToArray();
            _noiseBinStds = bins.Select(b => b[1]).ToArray();
        }
        else
        {
            _logger.LogWarning("unknown mode '{Mode}', defaulting to generative", _config.Mode);
        }
    }

    // loads the cleaned csv file into memory
    // expects columns: timestamp, wind speed, power, rotor speed, pitch angle, gear oil temp
    private void LoadReplayData()
    {
        if (!File.Exists(_config.ReplayFilePath))
        {
            _logger.LogError("replay file not found: {Path}", _config.ReplayFilePath);
            return;
        }

        var lines = File.ReadAllLines(_config.ReplayFilePath);
        // skip the header (first line)
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 6) continue;   // not enough columns, skip

            // try to parse each field – if any fail we just skip that row
            if (DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var wind) &&
                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var power) &&
                double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var rotor) &&
                double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var pitch) &&
                double.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out var temp))
            {
                _replayData.Add(new ReplayRecord
                {
                    Timestamp = DateTime.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    WindSpeed = wind,
                    ActivePower = power,
                    RotorSpeed = rotor,
                    PitchAngle = pitch,
                    GearboxOilTemp = temp
                });
            }
        }
        _logger.LogInformation("loaded {Count} replay records from {Path}", _replayData.Count, _config.ReplayFilePath);
    }

    // this is the main method called by the pipeline (or worker) every polling interval
    public Task<RawData> FetchAsync(CancellationToken cancellationToken)
    {
        RawData data;
        // if we're in replay mode and have data, use that, otherwise fall back to generative
        if (_config.Mode.Equals("Replay", StringComparison.OrdinalIgnoreCase) && _replayData.Any())
        {
            data = GenerateFromReplay();
        }
        else
        {
            data = GenerateFromParams();
        }
        return Task.FromResult(data);
    }

    // picks the next record from the replay list, adds a tiny bit of noise, and assigns a turbine id
    private RawData GenerateFromReplay()
    {
        // if something went wrong and we have no replay data, use generative as a backup
        if (_replayData.Count == 0)
            return GenerateFromParams();

        var rec = _replayData[_replayIndex];
        _replayIndex = (_replayIndex + 1) % _replayData.Count;   // loop around when we reach the end

        // pick the next turbine id in round‑robin fashion
        string turbineId = _turbineIds[_turbineIndex];
        _turbineIndex = (_turbineIndex + 1) % _turbineIds.Count;

        // add a small random variation so the data isn't exactly repetitive
        // 1% noise for most fields, small absolute for pitch and temp
        double noiseFactor = 0.01;
        double windNoise = rec.WindSpeed * (1 + (_random.NextDouble() - 0.5) * noiseFactor);
        double powerNoise = rec.ActivePower * (1 + (_random.NextDouble() - 0.5) * noiseFactor);
        double rotorNoise = rec.RotorSpeed * (1 + (_random.NextDouble() - 0.5) * noiseFactor);
        double pitchNoise = rec.PitchAngle + (_random.NextDouble() - 0.5) * 0.5;
        double tempNoise = rec.GearboxOilTemp + (_random.NextDouble() - 0.5) * 1.0;

        return new RawData
        {
            TurbineId = turbineId,
            Timestamp = DateTime.UtcNow,                     // simulate live time
            WindSpeed = windNoise,
            ActivePower = powerNoise,
            RotorSpeed = rotorNoise,
            PitchAngle = Math.Max(0, pitchNoise),                  // pitch can't be negative
            GearboxOilTemp = tempNoise,
            // keep the old fields for compatibility with existing code
            Vibration = _random.NextDouble() * 10,
            Temperature = tempNoise
        };
    }

    // creates a brand new data point using the statistical models from the config
    private RawData GenerateFromParams()
    {
        var gen = _config.Generative;
        // pick the next turbine id
        string turbineId = _turbineIds[_turbineIndex];
        _turbineIndex = (_turbineIndex + 1) % _turbineIds.Count;

        // 1. wind speed – using normal distribution for simplicity (we could switch to weibull later)
        double windSpeed = NextGaussian(gen.WindSpeed.Mean, gen.WindSpeed.StdDev);
        windSpeed = Math.Max(0, windSpeed);                        // no negative wind

        // 2. active power – piecewise linear power curve plus noise
        double power = 0;
        if (windSpeed < gen.PowerCurve.CutIn)
            power = 0;
        else if (windSpeed <= gen.PowerCurve.RatedWind)
        {
            double ratio = (windSpeed - gen.PowerCurve.CutIn) / (gen.PowerCurve.RatedWind - gen.PowerCurve.CutIn);
            power = ratio * gen.PowerCurve.RatedPower;
        }
        else if (windSpeed <= gen.PowerCurve.CutOut)
            power = gen.PowerCurve.RatedPower;
        else
            power = 0;

        // add noise based on which wind speed bin we're in, bins are guaranteed sorted from python analysis script
        double std = InterpolateNoise(windSpeed);
        if (std > 0)
            power += NextGaussian(0, std);
        power = Math.Max(0, power);

        // 3. rotor speed – linear up to saturation wind, then constant
        double rotorSpeed;
        if (windSpeed < gen.RotorSpeed.SaturationWind)
        {
            rotorSpeed = gen.RotorSpeed.MaxRpm * (windSpeed / gen.RotorSpeed.SaturationWind);
        }
        else
        {
            rotorSpeed = gen.RotorSpeed.MaxRpm;
        }
        rotorSpeed += NextGaussian(0, gen.RotorSpeed.MaxRpm * 0.02);   // small noise
        rotorSpeed = Math.Max(0, rotorSpeed);

        // 4. pitch angle – zero until start wind, then linear up to max
        double pitchAngle = 0;
        if (windSpeed > gen.PitchAngle.StartWind)
        {
            double range = gen.PowerCurve.CutOut - gen.PitchAngle.StartWind;
            if (range > 0)
            {
                pitchAngle = gen.PitchAngle.MaxAngle * (windSpeed - gen.PitchAngle.StartWind) / range;
                pitchAngle = Math.Min(pitchAngle, gen.PitchAngle.MaxAngle);
            }
        }
        pitchAngle += NextGaussian(0, 1.0);                            // a bit of noise
        pitchAngle = Math.Max(0, pitchAngle);

        // 5. gear oil temperature – just pick a random value within the observed range
        double temp = gen.GearOilTemp.Min + _random.NextDouble() * (gen.GearOilTemp.Max - gen.GearOilTemp.Min);

        return new RawData
        {
            TurbineId = turbineId,
            Timestamp = DateTime.UtcNow,
            WindSpeed = windSpeed,
            ActivePower = power,
            RotorSpeed = rotorSpeed,
            PitchAngle = pitchAngle,
            GearboxOilTemp = temp,
            Vibration = _random.NextDouble() * 10,
            Temperature = temp
        };
    }

    // generates a normally distributed random number using the box‑muller transform
    private double NextGaussian(double mean, double stdDev)
    {
        double u1 = 1.0 - _random.NextDouble();   // uniform in (0,1]
        double u2 = 1.0 - _random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
    // given a wind speed, finds the nearest noise bin and returns the standard deviation
    private double InterpolateNoise(double windSpeed)
    {
        // if we don't have noise bins (e.g., in replay mode or config missing), just return 0
        if (_noiseBinCenters == null || _noiseBinStds == null)
            return 0;

        var centers = _noiseBinCenters;
        var stds = _noiseBinStds;

        // binary search to find the closest bin centre
        int idx = Array.BinarySearch(centers, windSpeed);
        if (idx < 0) idx = ~idx;                    // bitwise complement gives insertion point
        if (idx >= centers.Length) idx = centers.Length - 1;
        if (idx < 0) idx = 0;

        return stds[idx];
    }     

    // simple class to hold one row of replay data
    private class ReplayRecord
    {
        public DateTime Timestamp { get; set; }
        public double WindSpeed { get; set; }
        public double ActivePower { get; set; }
        public double RotorSpeed { get; set; }
        public double PitchAngle { get; set; }
        public double GearboxOilTemp { get; set; }
    }
}