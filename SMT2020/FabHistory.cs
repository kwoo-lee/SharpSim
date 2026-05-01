using SharpSim;

namespace SMT2020;

public class FabHistory : IHistory
{
    public double TotalProcessingTime { get; set; } = 0;
    public double TotalIdleTime { get; set; } = 0;
    public double TotalMaintenanceTime { get; set; } = 0;

    public Dictionary<string, int> FabInByProduct { get; } = new ();
    public Dictionary<string, int> FabOutByProduct { get; } = new ();
    public List<LotTrace> Traces { get; } = [];

    public double WeeklyTotalCTSeconds { get; private set; }
    public int WIP { get; private set; }

    public void FabIn(Lot lot)
    {
        WIP++;
        FabInByProduct.TryGetValue(lot.ProductName, out int prev);
        FabInByProduct[lot.ProductName] = prev + 1;
    }

    public void FabOut(Lot lot, SimTime now)
    {
        WIP--;
        WeeklyTotalCTSeconds += (double)(now - lot.StartTime);

        FabOutByProduct.TryGetValue(lot.ProductName, out int prev);
        FabOutByProduct[lot.ProductName] = prev + 1;
    }

    public void EndStep(LotTrace trace)
    {
        Traces.Add(trace);
    }

    public void ResetWeekly()
    {
        FabInByProduct.Clear();
        FabOutByProduct.Clear();
        WeeklyTotalCTSeconds = 0;
    }
}
