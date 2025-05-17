using System.Timers;
using Timer = System.Timers.Timer;

public class AutomationScheduler
{
    private readonly AutomationEngine _engine;
    private readonly Timer _timer;

    public AutomationScheduler(AutomationEngine engine, int intervalSeconds)
    {
        _engine = engine;
        _timer = new Timer(intervalSeconds * 1000);
        _timer.Elapsed += async (s, e) => await _engine.ExecuteAllAsync();
    }

    public void Start()
    {
        Console.WriteLine("Scheduler started.");
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        Console.WriteLine("Scheduler stopped.");
    }
}
