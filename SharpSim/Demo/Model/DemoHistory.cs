using SharpSim;
namespace SharpSim.Demo;

public class DemoHistory : IHistory
{
    public double TotalWaitingTimeInQueue { get; set; } = 0;
    public int TotalProcessedJobs { get; set; } = 0;
    public int TotalProcessingTime { get; set; } = 0;
}