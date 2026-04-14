/*
 configuration classes for our simulated data source.
 
 where do these numbers come from?
 I analysed one year of real scada data from kelmarsh wind farm in UK (turbine 1, 2019).
 using python scripts, calculated statistics like:
   - wind speed distribution (mean, std dev, weibull params)
   - power curve shape (cut‑in, rated wind, cut-out)
   - how much scatter (noise) there is around the power curve at different wind speeds
   - rotor speed behaviour
   - pitch angle behaviour
   - gear oil temperature range
 
 these numbers are based on real turbine behaviour, so they're realistic.
 but only used one turbine for now – the system is designed to be scalable (simulate any number of turbines -> turbinecount in config),
 so we can add per‑turbine parameters later.
 
 the two modes:
   - replay: cycles through the actual cleaned csv data (kelmarsh turbine 1)
   - generative: creates synthetic data using the statistical models below
 
 weibull distribution: a common way to model wind speeds. the shape (k) and scale (c)
 parameters here are approximate – also store mean/std dev for a simpler normal model.
 */

namespace COMP702_WindTurbine.DataSources;

public class SimulatedDataSourceConfig
{
    //"Replay" or "Generative". replay reads a real csv file, generative makes new data
    public string Mode { get; set; } = "Replay";

    //how many turbines we want to simulate. the kelmarsh farm has 6, so default is 6
    public int TurbineCount { get; set; } = 6;

    //path to the csv file used in replay mode. placed it in the "data" folder
    public string ReplayFilePath { get; set; } = "data/turbine1_clean.csv";

    //all the generative parameters live here – only used when Mode = "Generative"
    public GenerativeConfig Generative { get; set; } = new();
}

public class GenerativeConfig
{
    public WindSpeedConfig WindSpeed { get; set; } = new();
    public PowerCurveConfig PowerCurve { get; set; } = new();
    public RotorSpeedConfig RotorSpeed { get; set; } = new();
    public PitchAngleConfig PitchAngle { get; set; } = new();
    public GearOilTempConfig GearOilTemp { get; set; } = new();
}

//wind speed distribution – we can use either normal or weibull
//the mean and std dev are directly from the data
//weibull is more accurate for wind, so included k and c
public class WindSpeedConfig
{
    public double Mean { get; set; } //average wind speed (m/s) from kelmarsh turbine 1
    public double StdDev { get; set; } //standard deviation (m/s)
    public double WeibullK { get; set; } //shape parameter from weibull fit – higher = more peaked
    public double WeibullC { get; set; } //scale parameter (m/s) – related to average wind speed
}

//power curve – the heart of turbine performance
//cut‑in, rated wind and rated power came from binning wind speed vs power
//cut‑out is assumed (25 m/s) because our data didn't have many high‑wind events
public class PowerCurveConfig
{
    public double CutIn { get; set; } //wind speed where turbine starts making power (m/s) – from data
    public double RatedWind { get; set; } //wind speed where it first hits rated power (m/s) – from data
    public double RatedPower { get; set; } //max power output (kW) – from kelmarsh static file: 2050 kW
    public double CutOut { get; set; } //wind speed where it shuts down (m/s) – typical value, not in our data

    //noise bins: for each 0.5 m/s wind speed bin  calculated the standard deviation
    //of power around the mean curve. this lets us add realistic scatter.
    //each inner list is [wind_centre, std_dev]
    public List<List<double>> NoiseBins { get; set; } = new();
}

//rotor speed vs wind – increases roughly linearly up to a point, then flattens
//max rpm is the 99th percentile from data (ignoring extreme outliers)
//saturation wind is where it stops increasing – from the binned averages
public class RotorSpeedConfig
{
    public double MaxRpm { get; set; } //maximum rotor speed (rpm) from kelmarsh turbine 1
    public double SaturationWind { get; set; } //wind speed where rotor speed plateaus (m/s)
}

//pitch angle – normally near zero, starts rising near rated wind to shed excess energy
//start wind came from data (where median pitch exceeded 0.5°)
//max angle is the 99th percentile – can hit 90° during shutdown, but normal operation lower
public class PitchAngleConfig
{
    public double StartWind { get; set; } //wind speed where pitch starts to increase (m/s)
    public double MaxAngle { get; set; } //max pitch angle in normal operation (degrees) – from data
}

//gear oil temperature – just used the observed range from kelmarsh turbine 1
//later, we might model it as a function of power, but for now uniform random within this range is fine
public class GearOilTempConfig
{
    public double Min { get; set; } //minimum temperature from data (°C)
    public double Max { get; set; } //maximum temperature from data (°C)
    public double Mean { get; set; } //average temperature (°C) – not used yet, but handy
}