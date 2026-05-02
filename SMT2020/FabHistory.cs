using SharpSim;

namespace SMT2020;

public class FabHistory : IHistory
{

#region [Base]
    private int weekNumber = 0;
    public string RunId { get; private set; }
    public string LogPath { get => Path.Combine("Results", RunId); }
#endregion [Base End]

    private List<Route> routes = [];
    public Dictionary<string, int> FabInByRoute { get; } = new ();
    public Dictionary<string, int> WIPByRoute { get; } = new ();
    public Dictionary<string, int> FabOutByRoute { get; } = new ();
    public Dictionary<string, double> TotalCycleTimeByRoute { get; } = new ();
    private List<ToolGroup> toolGroups = [];
    public List<int> ProcessCountByTG { get; } = new ();
    public List<double> TotalWaitTimeByTG { get; } = new ();
    public List<double> TotalProcessTimeByTG { get; } = new ();
    public Dictionary<string, int[]> WIPByRouteStep { get; } = new ();
    public Dictionary<string, int[]> ProcessCountByRouteStep { get; } = new ();
    public Dictionary<string, double[]> TotalWaitTimeByRouteStep { get; } = new ();
    public Dictionary<string, double[]> TotalProcessTimeByRouteStep { get; } = new ();
    public List<LotTrace> Traces { get; } = [];
    public int WIP { get => WIPByRoute.Values.Sum(); }
    
    public FabHistory()
    {
        RunId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(this.LogPath);
    }

#region [Logging]
    public void AddRoute(Route route)
    {
        routes.Add(route);
        string n = route.Name;
        FabInByRoute[n] = 0;
        WIPByRoute[n] = 0;
        FabOutByRoute[n] = 0;
        TotalCycleTimeByRoute[n] = 0;
    }

    public void AddToolGroup(ToolGroup tg)
    {
        toolGroups.Add(tg);
        if(ProcessCountByTG.Count == tg.Id)
        {
            ProcessCountByTG.Add(0);
            TotalWaitTimeByTG.Add(0);
            TotalProcessTimeByTG.Add(0);
        }
        else
        {
            throw new Exception("Tool Group ID Sync Error");
        }
    }

    public void FabIn(Lot lot)
    {
        WIPByRoute[lot.Route.Name] += 1;
        FabInByRoute[lot.Route.Name] += 1;
    }

    public void FabOut(Lot lot, SimTime now)
    {
        double ct = (double)(now - lot.StartTime);
        TotalCycleTimeByRoute[lot.Route.Name] += ct;

        WIPByRoute[lot.Route.Name] -= 1;
        FabOutByRoute[lot.Route.Name] +=  1;
    }

    public void StartStep(Lot lot)
    {
        string r = lot.Route.Name;
        int idx = lot.StepIndex;
        if (!WIPByRouteStep.TryGetValue(r, out var wips))
        {
            int n = lot.Route.Steps.Count;
            wips = new int[n];
            WIPByRouteStep[r] = wips;
            ProcessCountByRouteStep[r] = new int[n];
            TotalWaitTimeByRouteStep[r] = new double[n];
            TotalProcessTimeByRouteStep[r] = new double[n];
        }
        wips[idx] += 1;
    }

    public void EndStep(Lot lot)
    {
        var trace = lot.Trace;
        Traces.Add(trace);

        double wait = (double)(trace.DequeueTime - trace.EnqueueTime);
        double proc = (double)(trace.ProcessEndTime - trace.ProcessStartTime);

        int toolGroupId = lot.CurrentStep.ToolGroup.Id;
        ProcessCountByTG[toolGroupId] += 1;
        TotalWaitTimeByTG[toolGroupId] += wait;
        TotalProcessTimeByTG[toolGroupId] += proc;

        string r = lot.Route.Name;
        int idx = lot.StepIndex;
        WIPByRouteStep[r][idx] -= 1;
        ProcessCountByRouteStep[r][idx] += 1;
        TotalWaitTimeByRouteStep[r][idx] += wait;
        TotalProcessTimeByRouteStep[r][idx] += proc;
    }
#endregion [Logging End]

#region [Report]
    public void ReportWeekly()
    {
        weekNumber++;
        FabReport();
        ToolGroupReport();
        RouteReport();
        RouteStepReport();
        ResetWeekly();
    }
    
    private void FabReport()
    {
        string path = Path.Combine(this.LogPath, "FabWeekly.csv");
        bool needHeader = !File.Exists(path);

        int fabIn = FabInByRoute.Values.Sum();
        int fabOut = FabOutByRoute.Values.Sum();
        double avgCT = fabOut > 0
            ? TotalCycleTimeByRoute.Values.Sum() / fabOut / 86400.0
            : 0.0;

        using (var writer = new StreamWriter(path, append: true))
        {
            if (needHeader)
                writer.WriteLine("Week,FabIn,FabOut,AvgCT_Days,WIP");
            writer.WriteLine($"{weekNumber},{fabIn},{fabOut},{avgCT:F2},{WIP}");
        }
    }

    private void RouteReport()
    {
        string path = Path.Combine(this.LogPath, "Route.csv");
        bool needHeader = !File.Exists(path);

        using var writer = new StreamWriter(path, append: true);
        if (needHeader)
            writer.WriteLine("Week,Route,FabIn,FabOut,AvgCT_Days,WIP");

        foreach (var routeName in FabInByRoute.Keys)
        {
            int fabIn = FabInByRoute[routeName];
            int fabOut = FabOutByRoute[routeName];
            double avgCT = fabOut > 0
                ? TotalCycleTimeByRoute[routeName] / fabOut / 86400.0
                : 0.0;
            int wip = WIPByRoute[routeName];
            writer.WriteLine($"{weekNumber},{routeName},{fabIn},{fabOut},{avgCT:F2},{wip}");
        }
    }

    private void ToolGroupReport()
    {
        string path = Path.Combine(this.LogPath, "ToolGroup.csv");
        bool needHeader = !File.Exists(path);

        using var writer = new StreamWriter(path, append: true);
        if (needHeader)
            writer.WriteLine("Week,ToolGroup,WIP,ProcessCount,AvgWaitTime_min,AvgProcessTime_min");

        for(int toolGroupId = 0; toolGroupId < toolGroups.Count; toolGroupId++)
        {
            int wip = toolGroups[toolGroupId].LotQueue.Count + toolGroups[toolGroupId].Tools.Sum(t => t.AssignedLots.Count);
            int processCount = ProcessCountByTG[toolGroupId];
            double avgWait = processCount > 0 ? TotalWaitTimeByTG[toolGroupId] / processCount / 60.0: 0;
            double avgProc = processCount > 0 ? TotalProcessTimeByTG[toolGroupId] / processCount / 60.0 : 0;
            writer.WriteLine($"{weekNumber},{toolGroups[toolGroupId].Name},{wip},{processCount},{avgWait:F2},{avgProc:F2}");
        }
    }

    private void RouteStepReport()
    {
        string dir = Path.Combine(this.LogPath, "Route");
        Directory.CreateDirectory(dir);

        foreach (var route in routes)
        {
            string path = Path.Combine(dir, $"{route.Name}_{weekNumber}.csv");
            using var writer = new StreamWriter(path);
            writer.WriteLine("Step,ToolGroup,WIP,ProcessCount,AvgWaitTime_min,AvgProcessTime_min");

            bool seen = WIPByRouteStep.TryGetValue(route.Name, out var wips);
            int[]? pcs = seen ? ProcessCountByRouteStep[route.Name] : null;
            double[]? waits = seen ? TotalWaitTimeByRouteStep[route.Name] : null;
            double[]? procs = seen ? TotalProcessTimeByRouteStep[route.Name] : null;

            for (int i = 0; i < route.Steps.Count; i++)
            {
                var step = route.Steps[i];
                int wip = seen ? wips![i] : 0;
                int pc = seen ? pcs![i] : 0;
                double avgWait = pc > 0 ? waits![i] / pc / 60.0 : 0;
                double avgProc = pc > 0 ? procs![i] / pc / 60.0 : 0;
                writer.WriteLine($"{step.Order},{step.ToolGroup.Name},{wip},{pc},{avgWait:F2},{avgProc:F2}");
            }
        }
    }

    public void ResetWeekly()
    {
        foreach(var routeName in FabInByRoute.Keys)
        {
            FabInByRoute[routeName] = 0;
            FabOutByRoute[routeName] = 0;
            TotalCycleTimeByRoute[routeName] = 0;
        }

        for(int toolGroupId = 0; toolGroupId < toolGroups.Count; toolGroupId++)
        {
            ProcessCountByTG[toolGroupId] = 0;
            TotalWaitTimeByTG[toolGroupId] = 0;
            TotalProcessTimeByTG[toolGroupId] = 0;
        }

        foreach (var route in routes)
        {
            if (!ProcessCountByRouteStep.TryGetValue(route.Name, out var pcs)) continue;
            var waits = TotalWaitTimeByRouteStep[route.Name];
            var procs = TotalProcessTimeByRouteStep[route.Name];
            for (int i = 0; i < pcs.Length; i++)
            {
                pcs[i] = 0;
                waits[i] = 0;
                procs[i] = 0;
            }
        }
    }
#endregion
}
