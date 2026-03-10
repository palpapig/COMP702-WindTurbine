/*
purpose: defines the contract for the data accessor – the central hub that all other components use to read from and write to the database. every arrow touching the data accessor is represented here
what will be added later:
nothing, I think – the interface is already complete; implementations will be added in DataAccessor.
*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.DataAccess
{
    public interface IDataAccessor
    {
        Task StoreRawTelemetryAsync(RawTelemetry data); //write RAW data + extreme outlier flags
        Task StoreCleanedTelemetryAsync(CleanedTelemetry data); //write CLEANED data + kNN flags
        Task WriteAnalysisResultsAsync(int cleanedTelemetryId, TelemetryAnalysis analysis); //write metrics and alarms
        Task<BaselineCurve?> GetBaselineCurveAsync(string turbineId, string curveType); //read baseline curve for a turbine
        Task<List<TrainingData>> GetTrainingDataAsync(string turbineId); //read historical training data
        Task<List<RawTelemetry>> GetRawTelemetryForTurbineAsync(string turbineId, DateTime? from = null, DateTime? to = null); //read raw data (for graphs)
        Task<List<CleanedTelemetry>> GetCleanedTelemetryForTurbineAsync(string turbineId, DateTime? from = null, DateTime? to = null); //read cleaned data
        Task AddBaselineCurveAsync(BaselineCurve curve); //store a new baseline curve
        Task AddTrainingDataAsync(TrainingData trainingData); //store a reference to training data
        Task<List<Alert>> GetActiveAlarmsAsync(); //get active alarms for the UI
        Task<object> GetCurrentTurbineStatusAsync(); //get latest status of all turbines (for dashboard)
        Task<Dictionary<string, string>> GetConfigurationAsync(); //read all configuration entries
        Task UpdateConfigurationAsync(Dictionary<string, string> config); //replace all configuration entries
        Task AddTurbineAsync(Turbine turbine); //add a new turbine to the database
        Task UpdateTelemetryWithAnalysisAsync(int cleanedTelemetryId, TelemetryAnalysis analysis); //update a telemetry record with its analysis
    }
}