using COMP702_WindTurbine;
using COMP702_WindTurbine.services;
using COMP702_WindTurbine.database;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var monitoringDbConnection =
    Environment.GetEnvironmentVariable("ConnectionStrings__MonitoringDb")
    ?? builder.Configuration.GetConnectionString("MonitoringDb")
    ?? throw new InvalidOperationException(
        "Missing database connection string. Set environment variable 'ConnectionStrings__MonitoringDb'.");

builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseNpgsql(monitoringDbConnection));

builder.Services.AddScoped<DbService>();

builder.Services.AddSingleton<DataInput>();
builder.Services.AddSingleton<DataFormatter>();
builder.Services.AddSingleton<Benchmarker>();
builder.Services.AddSingleton<FailureDetection>();
builder.Services.AddHostedService<MonitoringWorker>();



var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
