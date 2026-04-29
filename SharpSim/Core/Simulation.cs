namespace SharpSim;


public interface ISimulation
{
    List<ISimNode> Nodes { get; }
    SimTime Now { get; }
    void Schedule(IEvent evt);
    void AddNode(ISimNode node);
    void Run(SimTime endOfSimulation);
    void Delay(SimTime delay, List<Action> actions);
    void DelayUntil(SimTime time, List<Action> actions);
}

public interface IHistory
{
}

public class Simulation : ISimulation
{
    private IEventList evtList;
    public SimTime Now { get; private set; } = new SimTime(0);
    public List<ISimNode> Nodes { get; private set; } = new List<ISimNode>();
    public Dictionary<string, long> EventCounts { get; private set; } = new Dictionary<string, long>();
    public Simulation(IEventList evtList)
    {
        this.evtList = evtList;
    }

    public void AddNode(ISimNode node)
    {
        Nodes.Add(node);
    }

    public void Schedule(IEvent evt)
    {
        evtList.Add(evt);
    }

    public virtual void Run(SimTime endOfSimulation)
    {
        foreach (var node in Nodes)
        {
            node.Initialize();
        }

        EventCounts.Clear();
        long totalEvents = 0;

        while (evtList.Count > 0)
        {
            var evt = evtList.RetrieveNext();
            if (evt is null || evt.Time > endOfSimulation)
                break;

            Now = evt.Time;

            var key = evt.GetType().Name;
            EventCounts[key] = EventCounts.TryGetValue(key, out var c) ? c + 1 : 1;
            totalEvents++;

            evt.Execute();
        }

        ReportEventCounts(totalEvents);
    }

    private void ReportEventCounts(long totalEvents)
    {
        LogHandler.Info($"=== Event call histogram (total: {totalEvents:N0}) ===");
        foreach (var kv in EventCounts.OrderByDescending(kv => kv.Value))
        {
            var pct = totalEvents == 0 ? 0.0 : (double)kv.Value / totalEvents * 100.0;
            LogHandler.Info($"  {kv.Key,-40} {kv.Value,12:N0}  ({pct,5:F1}%)");
        }
    }

    public void Delay(SimTime delay, List<Action> actions)
    {
        Schedule(new TimeDelayEvent(Now + delay, actions));
    }

    public void DelayUntil(SimTime time, List<Action> actions)
    {
        Schedule(new TimeDelayEvent(time, actions));
    }
}
