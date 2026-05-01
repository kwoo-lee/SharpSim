using System.Diagnostics;

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

public class Simulation(IEventList evtList) : ISimulation
{
    private IEventList evtList = evtList;
    public SimTime Now { get; private set; } = new SimTime(0);
    public List<ISimNode> Nodes { get; private set; } = new List<ISimNode>();
    public TimeSpan LastRunElapsed { get; private set; }
    public string RunId { get; } = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    public string LogPath { get => Path.Combine("Results", RunId); }

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
        // Create Log Folder
        Directory.CreateDirectory(this.LogPath);
        
        foreach (var node in Nodes)
        {
            node.Initialize();
        }

        var sw = Stopwatch.StartNew();

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
}
