/*
purpose: this is the heart of the windows service – a background worker that runs continuously. 
currently it just logs heartbeats to prove the service is alive. 
later it will orchestrate the whole data pipeline: fetch from data sources, process, and store.
what will be added later:
- inject a method which is a collection of all registered data sources (mock, possibly canary etc.) the monitoring worker will loop through them to fetch live data from every source 
- for each batch, call a method that takes a batch of raw telemetry, normalises units, runs outlier detectors and stores the results (raw with flags, cleaned with flags) through the data accessor. its the main preprocessing step
- maybe also trigger engines directly (or let the formatter do it)
- handle errors & logging
*/
using Microsoft.Extensions.Hosting; //provides backgroundservice & hostedservice interfaces
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace COMP702_WindTurbine.Workers
{
    public class MonitoringWorker : BackgroundService //backgroundservice is a base class for long-running services
    {
        private readonly ILogger<MonitoringWorker> _logger; //logger instance for this worker
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(10); //how often to log (later, how often to poll)

        public MonitoringWorker(ILogger<MonitoringWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) //this method runs when the service starts
        {
            _logger.LogInformation("Monitoring Worker started at: {time}", DateTimeOffset.Now);
            //loop until pressing ctrl + c
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker heartbeat at: {time}", DateTimeOffset.Now); //log a heartbeat
                await Task.Delay(_interval, stoppingToken); //wait for the interval, respecting the cancellation token
            }

            _logger.LogInformation("Monitoring Worker stopped at: {time}", DateTimeOffset.Now);
        }
    }
}