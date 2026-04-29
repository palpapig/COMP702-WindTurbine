namespace COMP702_WindTurbine;

using COMP702_WindTurbine.services;
using COMP702_WindTurbine.database;
using COMP702_WindTurbine.DataSources;
using COMP702_WindTurbine.ModelTraining;

public sealed class MonitoringWorker(
    IDataSource dataSource, // new: injected instead of DataInput
    DataFormatter dataFormatter,
    Benchmarker benchmarker,
    FailureDetection FailureDetection,

    ILogger<MonitoringWorker> logger,
    IServiceScopeFactory scopeFactory) : BackgroundService
//logger, etc. are automatically stored as private, readonly variables
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            DateTime lastTrainingCheckUtc = DateTime.MinValue;




            while (!stoppingToken.IsCancellationRequested)
            {

                logger.LogInformation("Processing new data started");
                //fetch realistic raw data from the simulated source
                var newRaw = await dataSource.FetchAsync(stoppingToken);
                //map to the old RawData structure (the formatter expects the old one)
                var oldRaw = new models.RawData
                {
                    TurbineId = newRaw.TurbineId,
                    Timestamp = newRaw.Timestamp,
                    //the properties WSSensor, etc., are computed from the new fields
                    //we just need to create an instance; the computed properties will be used
                };
                var telemetry = dataFormatter.FormatData(oldRaw);

                //add the extra fields from the new raw data
                telemetry.WindSpeed = newRaw.WindSpeed;
                telemetry.RotorSpeed = newRaw.RotorSpeed;
                telemetry.PowerOutput = newRaw.ActivePower;
                telemetry.Vibration = newRaw.Vibration;
                telemetry.Temperature = newRaw.Temperature;
                telemetry.PitchAngle = newRaw.PitchAngle;
                telemetry.GearboxOilTemp = newRaw.GearboxOilTemp;

                telemetry = benchmarker.BenchmarkData(telemetry);

                telemetry = FailureDetection.DetectFailure(telemetry);
                logger.LogWarning("Pipeline complete. id:{Id} power:{PowerOutput} efficiency:{Efficiency} alert:{StartedAlert}",
                telemetry.Id, telemetry.PowerOutput, telemetry.Efficiency, telemetry.StartedAlert);





                using (var scope = scopeFactory.CreateScope())
                {
                    var dbService = scope.ServiceProvider.GetRequiredService<DbService>();
                    await dbService.AddTelemetryAsync(telemetry);
                    await dbService.PrintDbAsync();
                }


                if (DateTime.UtcNow - lastTrainingCheckUtc >= TimeSpan.FromHours(1))
                {
                    lastTrainingCheckUtc = DateTime.UtcNow;

                    try
                    {
                        using var trainingScope = scopeFactory.CreateScope();

                        var trainingScheduleService = trainingScope.ServiceProvider.GetRequiredService<TrainingScheduleService>();
                        var modelTrainingService = trainingScope.ServiceProvider.GetRequiredService<ModelTrainingService>();

                        var dueTurbines = trainingScheduleService.GetTurbinesDueForTraining();

                        foreach (var turbineId in dueTurbines)
                        {
                            logger.LogInformation("Model retraining due for turbine {TurbineId}", turbineId);
                            await modelTrainingService.RunTrainingForTurbineAsync(turbineId, stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error while checking or running model retraining.");
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);


            }
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
    }
}