using SharpSim;

namespace SMT2020;

public class LotTrace
{
    public SimTime EnqueueTime{get; set;}
    public SimTime DequeueTime{get; set;}
    public SimTime EstimatedProcessEndTime {get;set;}
    public SimTime ProcessStartTime{get; set;}
    public SimTime ProcessEndTime {get; set;}

    public string AssginedTool {get; set;} = "";
    public void Assign(SimTime now, string toolName)
    {
        DequeueTime = now;
        AssginedTool = toolName;
    }
}