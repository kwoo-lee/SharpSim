using SharpSim;

namespace SMT2020;

public class Lot(int id, string name, string productName, Route route, int wafersPerLot, int priroity, SimTime startTime, SimTime endTime) 
    : SharpSim.SimObject(id, name)
{
    #region [Attributes]
    public string ProductName { get; private set; } = productName;
    public Route Route { get; private set; } = route;
    public int StepIndex { get; set; } = -1;
    public Step? CurrentStep { get => Route.Steps[StepIndex]; } 
    public int WafersPerLot { get; private set; } = wafersPerLot;
    public int Priority { get; private set; } = priroity;
    public SimTime StartTime { get; private set; } = startTime;
    public SimTime DueTime { get; private set; } = endTime;
    #endregion [Attributes End]

    #region [Status]
    public LotTrace Trace {get; set;} = new LotTrace();
    public LotStatus State { get; set; }
    public string Location { get; set; } = "";
    #endregion [Status End]

    public double GetProcessingTime()
    {
        double processingTime = CurrentStep.ProcessingTime.GetNumber();
        return processingTime;
    }
}