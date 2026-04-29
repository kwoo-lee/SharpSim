using SharpSim;

namespace SMT2020;

public class Step(int order, string description, ToolGroup toolGroup, ProcessingUnit processingUnit)
{
    public int Order { get; } = order;
    public string Description { get; } = description;
    public ToolGroup ToolGroup { get; } = toolGroup;
    public ProcessingUnit ProcessingUnit { get; } = processingUnit;
    public Distribution ProcessingTime { get; private set; } = new Const(0);
    public Distribution CascadingInterval { get; private set; } = new Const(0);
    public double ProcessingProbability { get; private set; } = 100.0;
    public int BatchMinimum { get; private set; }
    public int BatchMaximum { get; private set; }
    public double? MaximumWaitingTimeForMinBatch { get; private set; }
    public double? MaximumWaitingTimeForMaxBatch { get; private set; }
    public SetUp? SetUp { get; private set; }
    public uint StepForLTLDedication { get; private set; }
    public double ReworkProbability { get; private set; }
    public uint StepForRework { get; private set; }
    public uint StepForCriticalQueueTime { get; private set; }
    public Distribution? CriticalQueueTime { get; private set; }

    public void SetProcessingTime(Distribution processingTime, Distribution cascadingInterval, double processingProbability)
    {
        ProcessingTime = processingTime;
        CascadingInterval = cascadingInterval;
        ProcessingProbability = processingProbability;

        if (ToolGroup.Toolype == ToolType.Batch)
        {
            MaximumWaitingTimeForMinBatch = processingTime.Mean * 3.0 / 10.0;
            MaximumWaitingTimeForMaxBatch = processingTime.Mean * 1.0 / 10.0;
        }
    }

    public void SetSetUp(SetUp setUp) => SetUp = setUp;

    public void SetBatchSize(int min, int max)
    {
        BatchMinimum = min;
        BatchMaximum = max;
    }

    public void SetLTLDedication(uint step) => StepForLTLDedication = step;

    public void SetRework(uint step, double probability)
    {
        ReworkProbability = probability;
        StepForRework = step;
    }

    public void SetCriticalQueueTime(uint stepForCQT, double cqt)
    {
        StepForCriticalQueueTime = stepForCQT;
        CriticalQueueTime = new Const(cqt);
    }
}
