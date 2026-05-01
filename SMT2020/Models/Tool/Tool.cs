using SharpSim;

namespace SMT2020;

/// <summary>
/// This is tool for Table Type & Cascading
/// </summary>
public class Tool(Fab fab, FabHistory hist, int id, string name, ToolType type, ToolGroup toolGroup)
    : SimNode<Fab, FabHistory>(fab, hist, id, name)
{    
    #region [Attributes]
    protected double LoadingTime = toolGroup.LoadingTime;
    protected double UnloadingTime = toolGroup.UnloadingTiem;
    public ToolType Type {get; private set;} =  type;
    public ToolGroup ToolGroup { get; private set; } = toolGroup;
    #endregion [Attributes End]

    #region [Lots]
    public bool IsReserved { get; protected set;} = false;
    public List<Lot> AssignedLots {get; private set;} = [];
    protected List<Lot> StagedLots { get; private set; } = [];
    protected Dictionary<Lot, SimTime> RunningLots { get; private set; } = new();
    protected List<Lot> FinishedLots { get; private set; } = [];
    protected SimTime nextRunnableTime;
    #endregion [Lots End]

    #region Simulation Methods
    public override void SetState(Enum newState)
    {
        //_currentSetUpName = setUpName;
        //_lastLotName = lastLotName;

        var elapsedTime = Sim.Now - lastStateUpdatedTime;
        switch (this.State)
        {
            // case ToolState.Busy:
            //     _totalValueAddedTime += elapsedTime;
            //     break;
            // case ToolState.Breakdown:
            // case ToolState.PM:
            //     _totalDownTime += elapsedTime;
            //     break;
        }
        base.SetState(newState);
    }

    public override void Initialize()
    {
        base.Initialize();
        this.SetState(ToolState.Idle);
        //ReservedPM = null;
    }
    #endregion

    #region [Events]
    public virtual void AssignLot(Lot lot)
    {
        this.IsReserved = true;
        AssignedLots.Add(lot);
    }

    public virtual void LoadStart(Transport transport, Lot lot)
    {
        LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | LoadStart");
        Sim.Delay(this.LoadingTime, [() => { LoadFinish(lot); }]);    
    }

    protected virtual void LoadFinish(Lot lot)
    {
        this.Entities.Add(lot); // All Lots
        this.AssignedLots.Remove(lot);
        this.StagedLots.Add(lot);

        //LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | LoadFinish");

        if(this.State is ToolState.Idle || this.State is ToolState.Busy)
        {
            if(this.State is ToolState.Idle)
                this.SetState(ToolState.Busy);

            if(StagedLots.Count == 1)
            {
                if(Sim.Now > nextRunnableTime)
                {
                    ProcessStart(lot);
                }
                else
                {
                    Sim.Delay(nextRunnableTime - Sim.Now, [() => {ProcessStart(lot);}]);
                }
            }
        }
    }

    protected virtual void ProcessStart(SimObject simObject)
    {
        if(this.State is ToolState.Idle || this.State is ToolState.Busy)
        {
            Lot? lot = simObject as Lot;
            if(lot == null)
            {
                LogHandler.Error("ProcessStart: Wrong Sim Object Type");
                return;
            }
            else if (lot.CurrentStep == null)
            {
                LogHandler.Error($"ProcessStart: No Current Step {lot.Name}");
                return;
            }

            double processingTime = lot.GetProcessingTime();
            if(Type is ToolType.Table)
            {
                if(lot.CurrentStep.ProcessingUnit is ProcessingUnit.Wafer)
                {
                    processingTime = processingTime * lot.WafersPerLot;
                }
            }
            else if(Type is ToolType.Cascade)
            {
                if(lot.CurrentStep.ProcessingUnit is ProcessingUnit.Wafer)
                {
                    double interval = lot.CurrentStep.CascadingInterval.GetNumber(); // p = p + int * (w -1);
                    processingTime = processingTime + interval * (lot.WafersPerLot - 1);
                }
                else // p* = p * w
                    processingTime = processingTime * lot.WafersPerLot;
            }

            SimTime estimatedRunTime = Sim.Now + processingTime;

            this.StagedLots.Remove(lot);
            this.RunningLots.Add(lot, estimatedRunTime);

            Sim.DelayUntil(estimatedRunTime, [() => { ProcessFinish(lot); }]);

            LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | ProcessStart");

            // Lot Cascading Only
            if(this.Type == ToolType.Cascade && this.ToolGroup.ProcessingUnit == ProcessingUnit.Lot)
                nextRunnableTime = Sim.Now + lot.CurrentStep.CascadingInterval.GetNumber();
            else
                nextRunnableTime = estimatedRunTime;
            
            // Call next Lot
            this.IsReserved = false;
            Sim.MES.RequestNextLot(this);
        }
    }

    protected virtual void ProcessFinish(SimObject simObject)
    {
        Lot? lot = simObject as Lot;

        if(lot == null)
        {
            LogHandler.Error("ProcessStart: Wrong Sim Object Type");
            return;
        }
        
        LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | ProcessFinish");

        this.RunningLots.Remove(lot);
        this.FinishedLots.Add(lot);

        Sim.Delay(this.UnloadingTime, [() => { UnloadFinish(lot); }]);    
    }

    protected virtual void UnloadFinish(Lot lot)
    {
        this.Entities.Remove(lot);
        this.FinishedLots.Remove(lot);

        //LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | UnloadFinish");

        Sim.MES.SendLotToNextStep(lot);
    }
    #endregion [Process]
}
