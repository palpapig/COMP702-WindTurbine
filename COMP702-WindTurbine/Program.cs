using COMP702_WindTurbine;
using COMP702_WindTurbine.services;
using COMP702_WindTurbine.database;
using COMP702_WindTurbine.DataSources;
using COMP702_WindTurbine.ModelTraining;
using Microsoft.EntityFrameworkCore;
using COMP702_WindTurbine.models;

var builder = Host.CreateApplicationBuilder(args);
var monitoringDbConnection =
    Environment.GetEnvironmentVariable("ConnectionStrings__MonitoringDb")
    ?? builder.Configuration.GetConnectionString("MonitoringDb")
    ?? throw new InvalidOperationException(
        "Missing database connection string. Set environment variable 'ConnectionStrings__MonitoringDb'.");

builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseNpgsql(monitoringDbConnection));

builder.Services.AddScoped<DbService>();
//new data source (replaces DataInput)
builder.Services.AddSingleton<IDataSource, SimulatedLiveDataSource>();
//the config is automatically bound from appsetttings.json via the data source constructor
builder.Services.AddSingleton<DataFormatter>();
builder.Services.AddSingleton<Benchmarker>();
builder.Services.AddSingleton<FailureDetection>();
builder.Services.AddHostedService<MonitoringWorker>();
builder.Services.AddSingleton<PythonProcessService>();
builder.Services.AddSingleton<AlarmStateManager>();
builder.Services.AddSingleton<FailureDetectionAlarm>();
builder.Services.AddHttpClient<FailureDetection>(client =>

{
    client.BaseAddress = new Uri("http://127.0.0.1:8000/");
});

builder.Services.AddSingleton<FailureDetection2>(sp =>
{
    string modelPath = @"TrainedModel\model.onnx";

    if (!File.Exists(modelPath))
    {
        throw new FileNotFoundException($"ONNX model not found: {Path.GetFullPath(modelPath)}");
    }

    var alarmService = sp.GetRequiredService<FailureDetectionAlarm>();
    return new FailureDetection2(modelPath, alarmService);
});

builder.Services.Configure<FailureDetectionSettings>(
    builder.Configuration.GetSection("FailureDetectionSettings")
);




//######## This is for auto-training. not used anymore ##########
//builder.Services.AddSingleton<ModelTrainingConfigService>();
//builder.Services.AddSingleton<TrainingScheduleService>();

//builder.Services.AddHttpClient<ModelTrainingService>(client =>
//{
//    client.BaseAddress = new Uri("http://127.0.0.1:8000/");
//});




var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
    await db.Database.MigrateAsync();
    await db.Database.ExecuteSqlRawAsync(
        "SELECT setval(pg_get_serial_sequence('\"TurbineData\"','Id'), COALESCE(MAX(\"Id\"), 0) + 1, false) FROM \"TurbineData\";");
}

await host.RunAsync();
