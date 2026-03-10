/*
purpose: implements the IDataAccessor interface. this class will eventually contain all database operations. currently it's a stub
what will be added later:
- inject a MonitoringDbContext (or IServiceScopeFactory)
- implement each method with actual EF (entity framework) core queries -> the way we interact with the database using C# LINQ, saves from having to write raw sql
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.DataAccess
{
    public class DataAccessor : IDataAccessor
    {
        //ALL methods are placeholders - later they'll contain real database logic
        public Task StoreRawTelemetryAsync(RawTelemetry data) => throw new NotImplementedException();
        public Task StoreCleanedTelemetryAsync(CleanedTelemetry data) => throw new NotImplementedException();
        public Task WriteAnalysisResultsAsync(int cleanedTelemetryId, TelemetryAnalysis analysis) => throw new NotImplementedException();
        public Task<BaselineCurve?> GetBaselineCurveAsync(string turbineId, string curveType) => throw new NotImplementedException();
        public Task<List<TrainingData>> GetTrainingDataAsync(string turbineId) => throw new NotImplementedException();
        public Task<List<RawTelemetry>> GetRawTelemetryForTurbineAsync(string turbineId, DateTime? from, DateTime? to) => throw new NotImplementedException();
        public Task<List<CleanedTelemetry>> GetCleanedTelemetryForTurbineAsync(string turbineId, DateTime? from, DateTime? to) => throw new NotImplementedException();
        public Task AddBaselineCurveAsync(BaselineCurve curve) => throw new NotImplementedException();
        public Task AddTrainingDataAsync(TrainingData trainingData) => throw new NotImplementedException();
        public Task<List<Alert>> GetActiveAlarmsAsync() => throw new NotImplementedException();
        public Task<object> GetCurrentTurbineStatusAsync() => throw new NotImplementedException(); //placeholder for a future DTO (data transfer object -> class that carries data between processes, to ensure the API returns a well defined structure that the react frontend can rely on)
        public Task<Dictionary<string, string>> GetConfigurationAsync() => throw new NotImplementedException();
        public Task UpdateConfigurationAsync(Dictionary<string, string> config) => throw new NotImplementedException();
        public Task AddTurbineAsync(Turbine turbine) => throw new NotImplementedException();
        public Task UpdateTelemetryWithAnalysisAsync(int cleanedTelemetryId, TelemetryAnalysis analysis) => throw new NotImplementedException();
    }
}