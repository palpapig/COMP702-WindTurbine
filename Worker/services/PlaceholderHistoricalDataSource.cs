namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;
using System.Globalization;


///<summary>
///Provides bulk data access the same data source which the OCP UA live data simulator uses.
///</summary>
public sealed class PlaceholderHistoricalDataSource(
    ILogger<MonitoringWorker> logger
)
{
    //Hard coded the location of the csv which OCP UA reads from
    readonly string csvPath = "../Simulator/data/turbine4_2017-2022.csv";
    //Hard coded the contents of each column in the csv.
    readonly int timestampCol = 0;
    readonly int windSpeedCol = 1;
    readonly int correctedWindSpeedCol = 2;
    readonly int powerOutputCol = 3;
    readonly int minimumPowerOutputCol = 4;
    readonly int rotorSpeedCol = 5;
    readonly int generatorSpeedCol = 6;
    readonly int pitchAngleCol = 7;
    readonly int gearboxOilTempCol = 8;
    //These columns exist in the CSV but are unused here
    // readonly int gearOilInletTempCol = 9;
    // readonly int rearBearingTempCol = 10;
    // readonly int gearOilPumpPressureCol = 11;
    // readonly int generatorBearingFrontTempCol = 12;
    // readonly int gearOilInletPressureCol = 13;
    // readonly int nacelleTemCol = 14;

    public List<TurbineTelemetry> GetEarliestTurbineData(int monthsPeriod = 12){
        string[][] allRows = File.ReadLines(csvPath)
            .Skip(1)
            .Select(line => line.Split(','))
            .ToArray();
        
        string startTimestamp = allRows.MinBy(r => r[timestampCol])[timestampCol];
        DateTime startDate = DateTime.Parse(startTimestamp, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        DateTime endDate = startDate.AddMonths(monthsPeriod);

        string[][] rows = allRows
            .Where(r => startDate < DateTime.Parse(r[timestampCol]) && DateTime.Parse(r[timestampCol]) < endDate)
            .ToArray();

        return ConvertCsvToTelemetry(rows);
    }

    ///<summary>
    ///Gets data for a specific time period. Only gets data from turbine WT-004
    ///</summary>
    public List<TurbineTelemetry> GetHistoricalTurbineData(int monthsPeriod, DateTime endDate)
    {
        DateTime startDate = endDate.AddMonths(-monthsPeriod);
        var rows = File.ReadLines(csvPath)
            .Skip(1)
            .Select(line => line.Split(','))
            .Where(r => startDate < DateTime.Parse(r[timestampCol]) && DateTime.Parse(r[timestampCol]) < endDate) //only keep if between enddate and 3 months before enddate
            .ToArray();
        
        return ConvertCsvToTelemetry(rows);
    }
    private List<TurbineTelemetry> ConvertCsvToTelemetry(String[][] inputCsv)
    {
        List<TurbineTelemetry> telemetry = [];
        for (int i = inputCsv.Length - 1; i >= 1; i--)
        {
            string[] row = inputCsv[i];
            telemetry.Add(new TurbineTelemetry
            {
                TurbineId = "WT-004",
                Timestamp = DateTime.SpecifyKind(
    DateTime.Parse(row[timestampCol]), 
    DateTimeKind.Utc),
                WindSpeed = double.Parse(row[windSpeedCol]),
                CorrectedWindSpeed = double.Parse(row[correctedWindSpeedCol]),
                PowerOutput = double.Parse(row[powerOutputCol]),
                MinimumPowerOutput = double.Parse(row[minimumPowerOutputCol]),
                RotorSpeed = double.Parse(row[rotorSpeedCol]),
                GeneratorSpeed = double.Parse(row[generatorSpeedCol]),
                PitchAngle = double.Parse(row[pitchAngleCol]),
                GearboxOilTemp = double.Parse(row[gearboxOilTempCol]),
            });
        }

        return telemetry;

    }

}
