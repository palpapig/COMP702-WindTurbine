namespace COMP702_WindTurbine;
using COMP702_WindTurbine.services;
using COMP702_WindTurbine.database;


public sealed class MonitoringWorker(
    DataInput dataInput,
    DataFormatter dataFormatter,
    Benchmarker benchmarker,
    FailureDetection FailureDetection,
    ILogger<MonitoringWorker> logger,
    IServiceScopeFactory scopeFactory ) : BackgroundService
    //logger, etc. are automatically stored as private, readonly variables
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                logger.LogInformation("Processing new data started");
                var rawData = dataInput.GetDataRow();

                var telemetry = dataFormatter.FormatData(rawData);

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