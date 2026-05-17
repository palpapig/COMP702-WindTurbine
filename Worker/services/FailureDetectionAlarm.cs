using COMP702_WindTurbine.models;
using Microsoft.Extensions.Options;
namespace COMP702_WindTurbine.services;


/*
    FailureDetectionAlarm is responsible for applying the alarm logic
    after the model calculates the residual.

    It uses EWMA to smooth the residual values over time, then compares
    the EWMA value against fixed upper and lower control limits UCL and LCL.

    Alarm levels:
    - A1 is triggered when EWMA goes outside the control limits.
    - A2 is triggered when A1 happens repeatedly for the required count.

    The alarm state is stored per turbine using AlarmStateManager,
    so each turbine keeps its own EWMA history and A1 count.
*/

public class FailureDetectionAlarm
{
    private readonly AlarmStateManager _stateManager;

    private readonly AlarmSettings _settings;




    public FailureDetectionAlarm(AlarmStateManager stateManager, IOptions<FailureDetectionSettings> options)
    {
        _stateManager = stateManager;
        _settings = options.Value.Alarm;
       

  
    }

    public Alarm Evaluate(string turbineId, double residual)
    {
        // Get saved turbine alarm state
        AlarmState state = _stateManager.Get(turbineId);

        double residualBias = 0;

        // fetch bias for this turbine if exists, otherwise default to 0 
        if (_settings.TurbineResidualBiases.TryGetValue(turbineId, out double storedBias))
        {
            residualBias = storedBias;
        }
    

      // Adjust residual by subtracting the bias
     double adjustedResidual = residual - residualBias;


        double previousEwma = state.LastEwma;

        // Calculate EWMA
        double ewma = (_settings.EwmaLambda * adjustedResidual) + ((1 - _settings.EwmaLambda) * previousEwma);

        // If residualStd is null, use 0
        double std = _settings.ResidualStd;

        // Control limits
        double ucl = _settings.ControlLimitK * std;
        double lcl = -ucl;

        // A1 alarm: EWMA is outside the control limits
        bool a1Triggered = std > 0 && (ewma > ucl || ewma < lcl);

        // Count consecutive A1 alarms
        int consecutiveA1Count;

        if (a1Triggered)
        {
            consecutiveA1Count = state.ConsecutiveA1Count + 1;
        }
        else
        {
            consecutiveA1Count = 0;
        }

        // A2 alarm: repeated 3 A1 alarms
        bool a2Triggered = consecutiveA1Count >= _settings.A1RequiredCount;


        _stateManager.Update(
            turbineId,
            ewma,
            consecutiveA1Count,
            std
        );

        return new Alarm
        {
            Residual = adjustedResidual,
            EWMA = ewma,
            UCL = ucl,
            LCL = lcl,
            A1Triggered = a1Triggered,
            A2Triggered = a2Triggered,
            ConsecutiveA1Count = consecutiveA1Count
        };
    }
}