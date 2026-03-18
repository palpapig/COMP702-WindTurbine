using COMP702_WindTurbine.Alerting;
using COMP702_WindTurbine.DataSources;
using COMP702_WindTurbine.Infrastructure;
using COMP702_WindTurbine.Pipeline;
using COMP702_WindTurbine.Prediction;
using COMP702_WindTurbine.Processing;
using COMP702_WindTurbine.Workers;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseInMemoryDatabase("MonitoringDb"));

builder.Services.AddSingleton<IDataSource, MockDataSource>();
builder.Services.AddSingleton<IDataFormatter, DefaultFormatter>();
// builder.Services.AddSingleton<IPredictionEngine, RulePredictionEngine>();  //temp commented out
builder.Services.AddSingleton<PredictionEngineTest>(); // testing prediction engine API
builder.Services.AddHttpClient<IPredictionEngine, PythonPredictionEngine>(client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:8000/");
});

builder.Services.AddSingleton<AlertManager>();
builder.Services.AddSingleton<PipelineOrchestrator>();

builder.Services.AddSingleton<MetricsCollector>();
builder.Services.AddSingleton<HealthService>();
builder.Services.AddSingleton<RetryPolicy>();
builder.Services.AddSingleton<PollingScheduler>();

builder.Services.AddHostedService<MonitoringWorker>();

var host = builder.Build();

// testing prediction engine API
using (var scope = host.Services.CreateScope())
{
    var test = scope.ServiceProvider.GetRequiredService<PredictionEngineTest>();
    await test.RunAsync(CancellationToken.None);
}


host.Run();


