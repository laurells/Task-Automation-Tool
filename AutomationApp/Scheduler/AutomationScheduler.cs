using System.Timers;
using Timer = System.Timers.Timer;
using AutomationApp.Core;
using AutomationApp.Services;

public class AutomationScheduler
{
    private readonly AutomationEngine _engine;
    private readonly Timer _timer;
    private readonly Logger _logger;


    public AutomationScheduler(AutomationEngine engine, int intervalSeconds, Logger logger)
    {
        _engine = engine;
        _timer = new Timer(intervalSeconds * 1000);
        _timer.Elapsed += async (s, e) => await _engine.ExecuteAllAsync();
        _logger = logger;
    }

    public void Start()
    {
        _logger.LogInfo("Scheduler started.");
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _logger.LogInfo("Scheduler stopped.");
    }
}
