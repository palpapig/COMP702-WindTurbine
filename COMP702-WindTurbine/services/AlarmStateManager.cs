using COMP702_WindTurbine.models;

namespace COMP702_WindTurbine.services;

public class AlarmState
{
    public double LastEwma { get; set; } = 0.0;
    public int ConsecutiveA1Count { get; set; } = 0;
    public double ResidualStd { get; set; } = 0.0;
}

public class AlarmStateManager
{
    private readonly Dictionary<string, AlarmState> _states = new();

    public AlarmState Get(string turbineId)
    {
        if (!_states.ContainsKey(turbineId))
        {
            _states[turbineId] = new AlarmState();
        }

        return _states[turbineId];
    }

    public void Update(string turbineId, double lastEwma, int consecutiveA1Count, double residualStd)
    {
        AlarmState state = Get(turbineId);

        state.LastEwma = lastEwma;
        state.ConsecutiveA1Count = consecutiveA1Count;
        state.ResidualStd = residualStd;
    }
}