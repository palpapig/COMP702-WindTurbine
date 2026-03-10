/*
purpose: entry point for the windows service. it sets up the dependency injection container and starts the hosted service (the monitoring worker)
what will be added later:
- register IDataAccessor, IDataSource implementations, outlier detectors, engines & the pipeline orchestrator
- configure the dbcontext (with connection string from config)
*/

using COMP702_WindTurbine.Workers; //brings in the MonitoringWorker class
using Microsoft.Extensions.DependencyInjection; //gives us the ServiceCollection for dependency injection
using Microsoft.Extensions.Hosting; //provides the Host class to build & run the service

var builder = Host.CreateApplicationBuilder(args); //creates a default host builder with configuration, logging etc.

builder.Services.AddHostedService<MonitoringWorker>(); //registers the MonitoringWorker as a hosted service - this is what runs in the background

var host = builder.Build();
host.Run();