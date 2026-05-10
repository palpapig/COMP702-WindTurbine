using COMP702_WindTurbine.models;
using Microsoft.Extensions.Options;
namespace COMP702_WindTurbine.services;


public class FailureDetectionAlarm
{
    private readonly AlarmStateManager _stateManager;

    private readonly AlarmSettings _settings;

    //  double _ewmaLambda;
    //  private double _controlLimitK;
    // private double _residualStd;
    //private int _a2ConsecutiveA1Count;


    public FailureDetectionAlarm(AlarmStateManager stateManager, IOptions<FailureDetectionSettings> options)
    {
        _stateManager = stateManager;
        _settings = options.Value.Alarm;

        // _settings = options.Value.Alarm;
        //_ewmaLambda = _settings.EwmaLambda;
        //_controlLimitK = _settings.ControlLimitK;
        //_residualStd = _settings.ResidualStd;
        //_a2ConsecutiveA1Count = _settings.A2ConsecutiveA1Count;
    }

    public Alarm Evaluate(string turbineId, double residual)
    {
        // Get saved turbine alarm state
        AlarmState state = _stateManager.Get(turbineId);


        double previousEwma = state.LastEwma;

        // Calculate EWMA
        double ewma = (_settings.EwmaLambda * residual) + ((1 - _settings.EwmaLambda) * previousEwma);

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

        // A2 alarm: repeated A1 alarms
        bool a2Triggered = consecutiveA1Count >= _settings.A1RequiredCount;

        // Save updated state
        _stateManager.Update(
            turbineId,
            ewma,
            consecutiveA1Count,
            std
        );

        return new Alarm
        {
            Residual = residual,
            Ewma = ewma,
            Ucl = ucl,
            Lcl = lcl,
            A1Triggered = a1Triggered,
            A2Triggered = a2Triggered,
            ConsecutiveA1Count = consecutiveA1Count
        };
    }
}