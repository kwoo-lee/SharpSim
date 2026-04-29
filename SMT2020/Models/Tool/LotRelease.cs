using DocumentFormat.OpenXml.Spreadsheet;
using SharpSim;

namespace SMT2020;

public class LotRelease(Fab fab, FabHistory hist, int id, string name) 
    //: Tool(fab, hist, id, name, ToolType.LotRelease)
    : SimNode<Fab, FabHistory>(fab, hist, id, name)
{
    public static int LastLotId = 0;
    public Dictionary<string, List<ReleasePlan>>? ReleasePlanByRoute { get; private set; }
    public Dictionary<string, List<Lot>>? FutureLotsByRoute { get; private set; }
    public string Location { get; private set; } = "";

    #region [Setters]
    public void SetReleasePlan(Dictionary<string, List<ReleasePlan>> releasePlanByRoute) =>
        ReleasePlanByRoute = releasePlanByRoute;

    public void SetFutureLotList(Dictionary<string, List<Lot>> futureLotsByRoute) =>
        FutureLotsByRoute = futureLotsByRoute;
    public void SetLocation(string location) =>
        Location = location;
    #endregion [Setters End]
    public override void Initialize()
    {
        base.Initialize();

        // Release type 1. Distribution based Release
        if(ReleasePlanByRoute != null)
        {
            foreach (var (routeName, plans) in ReleasePlanByRoute)
            {
                foreach(ReleasePlan plan in plans)
                {
                    var arrivalTime = new SimTime((plan.StartDateTime - Sim.StartDateTime).TotalSeconds);
                    Sim.Delay(arrivalTime, new List<Action>() { () => { ReleaseByPlan(plan); } });

                    break; // Temp
                }

                break; // Temp
            }
        }
        
        if(FutureLotsByRoute != null)
        {
            // Release type 2. Lot by Lot Release
            foreach (var (routeName, plans) in FutureLotsByRoute)
            {
                if(plans.Count > 0)
                {
                    Sim.DelayUntil(plans[0].StartTime, new List<Action>() { () => { ReleaseByLotList(routeName); } });
                }
            }
        }
    }

    private void ReleaseByPlan(ReleasePlan plan)
    {
        for(int i = 0; i < plan.LotByRelease; i++)
        {
            Lot lot = new Lot(
                id: ++LastLotId, 
                name: plan.LotType + $"_{++plan.Count}", 
                productName: plan.ProductName, 
                route: plan.Route, 
                wafersPerLot: plan.WafersPerLot, 
                priroity: plan.Priority, 
                startTime: Sim.Now,
                endTime: Sim.Now + plan.CycleTime
            );

            ReleaseLot(lot);
        }

        // -- Temp --
        if(Sim.Now > 86400 * 30)
            return; 
        // -- Temp --

        var delayTime = plan.Dist.GetNumber();
        Sim.Delay(delayTime, new List<Action>() { () => { ReleaseByPlan(plan); }} );
    }
    
    private void ReleaseByLotList(string routeName)
    {
        if(FutureLotsByRoute is null) return;
        else if(FutureLotsByRoute.TryGetValue(routeName, out List<Lot>? lots) && lots.Count > 0)
        {
            Lot lot = lots[0];
            FutureLotsByRoute[routeName].RemoveAt(0);

            ReleaseLot(lot);

            if (FutureLotsByRoute[routeName].Count > 0)
            {
                SimTime arrivalTime = FutureLotsByRoute[routeName][0].StartTime;
                Sim.DelayUntil(arrivalTime, new List<Action>() { () => { ReleaseByLotList(routeName); } });
            }
            else
            {
                LogHandler.Info($"{Sim.Now, -11:F1} | Lot Release Finish {routeName}");
            }
        }
    }

    private void ReleaseLot(Lot lot)
    {
        lot.Location = this.Location;

        LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | Lot Release");

        Sim.MES.SendLotToNextStep(lot);
    }
}