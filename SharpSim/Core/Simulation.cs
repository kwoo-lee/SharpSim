using System.Diagnostics;

namespace SharpSim;


public interface ISimulation
{
    List<ISimNode> Nodes { get; }
    SimTime Now { get; }
    void Schedule(IEvent evt);
    void AddNode(ISimNode node);
    void Run(SimTime endOfSimulation, SimTime warmUpPeriod);
    void Delay(SimTime delay, List<Action> actions);
    void DelayUntil(SimTime time, List<Action> actions);
}

public interface IHistory
{
    void InitializeReport(SimTime now);
    void ReportWeekly(SimTime now);
}

public class Simulation(IEventList evtList, IHistory history) : ISimulation
{
    private IEventList evtList = evtList;
    public SimTime Now { get; private set; } = new SimTime(0);
    public List<ISimNode> Nodes { get; private set; } = new List<ISimNode>();
    public TimeSpan LastRunElapsed { get; private set; }
    public IHistory History { get; } = history;

    public void AddNode(ISimNode node)
    {
        Nodes.Add(node);
    }

    public void Schedule(IEvent evt)
    {
        evtList.Add(evt);
    }

    public virtual void Run(SimTime endOfSimulation, SimTime warmUpPeriod = default)
    {
        foreach (var node in Nodes)
        {
            node.Initialize();
        }

        var sw = Stopwatch.StartNew();
        this.Delay(warmUpPeriod, [OnWarmUpEnd]);
        while (evtList.Count > 0)
        {
            var evt = evtList.RetrieveNext();
            if (evt is null || evt.Time > endOfSimulation)
                break;

            Now = evt.Time;
            evt.Execute();
        }

        sw.Stop();
        LastRunElapsed = sw.Elapsed;
        LogHandler.Info($"Simulation finished in {LastRunElapsed.TotalSeconds:F3} s ({LastRunElapsed})");
    }

    public void Delay(SimTime delay, List<Action> actions)
    {
        Schedule(new TimeDelayEvent(Now + delay, actions));
    }

    public void DelayUntil(SimTime time, List<Action> actions)
    {
        Schedule(new TimeDelayEvent(time, actions));
    }

    protected virtual void OnWarmUpEnd()
    {
        History.InitializeReport(this.Now);
        this.Delay(604800.0, [WeeklyReport]);
    }

    protected virtual void WeeklyReport()
    {
        History.ReportWeekly(this.Now);
        this.Delay(604800.0, [WeeklyReport]);
    }
}
