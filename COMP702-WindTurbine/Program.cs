using COMP702_WindTurbine;
using COMP702_WindTurbine.services;
using COMP702_WindTurbine.database;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<MonitoringDbContext>(options =>
    options.UseInMemoryDatabase("MonitoringDb"));
    //options.UseSqlServer(CONNECTION_STRING_HERE)); sql server not set up

    //e.g.
    //options.UseSqlServer("Server=localhost;Database=MyDb;Trusted_Connection=True;"));

builder.Services.AddScoped<DbService>();

builder.Services.AddSingleton<DataInput>();
builder.Services.AddSingleton<DataFormatter>();
builder.Services.AddSingleton<Benchmarker>();
builder.Services.AddSingleton<FailureDetection>();
builder.Services.AddHostedService<MonitoringWorker>();



var host = builder.Build();
host.Run();
