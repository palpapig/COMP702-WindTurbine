namespace COMP702_WindTurbine.Infrastructure;

public sealed class MetricsCollector
{
    private long _signalsProcessed;
    private long _alarmsTriggered;

    public long SignalsProcessed => Interlocked.Read(ref _signalsProcessed);
    public long AlarmsTriggered => Interlocked.Read(ref _alarmsTriggered);

    public void IncrementSignalsProcessed() => Interlocked.Increment(ref _signalsProcessed);

    public void IncrementAlarmsTriggered() => Interlocked.Increment(ref _alarmsTriggered);
}
