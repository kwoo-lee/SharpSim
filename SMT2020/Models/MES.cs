using SharpSim;

namespace SMT2020;

public class MES(Fab fab, FabHistory hist, int id, string name) : SimNode<Fab, FabHistory>(fab, hist, id, name)
{
    private Random r = new Random();

#region [Manufacturing Information]
    public List<string> Products { get;  set; } = [];
    public Dictionary<string, Route> Routes { get;  } = new ();
    public List<ToolGroup> ToolGroups { get;  set; } = [];
    public Dictionary<string, ToolGroup> ToolGroupByName { get;  set; } = new ();
#endregion [Manufacturing Information End]

#region [Dispatch]
    private IDispatcher dispatcher = new Dispatcher();
    private List<int> dispatchToolGroups = [];
    private double dispatchDelay = 1;
#endregion [Dispatch End]

    public List<Lot> FabOutLots { get; } = [];

#region [Initialize]
    public void AddToolGroup(ToolGroup toolGroup, int numberOfTools)
    {
        ToolGroups.Add(toolGroup);
        ToolGroupByName[toolGroup.Name] = toolGroup;

        // Generate Tools
        if(toolGroup.Name != "Delay_32")
            toolGroup.AddTool(Sim, History, numberOfTools);
    }
#endregion

    public void SendLotToNextStep(Lot lot)
    {
        if (lot.StepIndex == -1) // Fab In
        {
            lot.StepIndex++;
            //NewLot(ㅇㅇlot);
        }
        else
        {
            //Step currentStep = lot.CurrentStep;
            //EndStep(timeNow, lot);

            double reworkRatio = lot.CurrentStep.ReworkProbability;
            double probability = r.NextDouble();
            probability = 1;
            if (probability >= reworkRatio) // Proceed to Next Step
            {
                // lot.RemainingProcessingTime -= lot.Route[lot.CurrentStep].ProcessingTime.Mean;
                // if (lot.RemainingProcessingTime < 0)
                //     lot.RemainingProcessingTime = 0;
                lot.StepIndex++;
            }
            else // Rework, Go back to Rework Step
            {
                throw new NotImplementedException();
                // for (uint i = lot.ReworkStep; i < lot.Step; i++)
                // {
                //     var mean = lot.Route[lot.Step].ProcessingTime.Mean;
                //     lot.RemainingProcessingTime += mean;
                // }
                // lot.StepIndex = lot.ReworkStep;
                // lot.NeedRework = false;
            }
        }

        if (lot.Route.Count > lot.StepIndex) // Next Step
        {
            Step nextStep = lot.CurrentStep;
            ToolGroup nextTG = nextStep.ToolGroup;

            LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | ({nextStep.Order}){nextStep.Description}");
            
            double samplingPct = r.NextDouble();
            if(samplingPct > nextStep.ProcessingProbability)
            {
                SendLotToNextStep(lot); // Skip the current Step
                return;
            }

            if (nextTG.Name == "Delay_32")
            {
                double delayTime = nextStep.ProcessingTime.GetNumber();
                Sim.Delay(delayTime, new List<Action>() { () => { SendLotToNextStep(lot); } });
                return;
            }

            lot.EnqueueTime = Sim.Now;
            nextTG.LotQueue.Add(lot);
            if(!dispatchToolGroups.Contains(nextTG.Id))
            {
                if(dispatchToolGroups.Count == 0) 
                    Sim.Delay(dispatchDelay, [() => { Dispatch(); }]);

                dispatchToolGroups.Add(nextTG.Id);
            }
            
            // TBDs
            // StartStep(timeNow, lot, nextStep);

            // ToolGroup nextToolGroup = _toolGroups[nextStep.ToolGroupName];
            // nextToolGroup.ExternalTransition(timeNow, new SimPort(TGExtPort.NewJob, foup));
        }
        else // Finish. Go To Complete
        {
            LogHandler.Debug($"{Sim.Now, -11:F1} | {this.Name, -21} | {lot.Name, -21} | FabOut");
            FabOutLots.Add(lot);

            // TBDs
            // Complete complete = _completes.Find(x => x.Spec.RoutePlans.ContainsKey(lot.Route));
            // RequestOHT(timeNow, foup.CurrentPort, complete, foup);
        }
    }

    public void RequestNextLot(Tool tool)
    {
        ToolGroup toolGroup = tool.ToolGroup;
        if(toolGroup.LotQueue.Count > 0)
        {
            if(!dispatchToolGroups.Contains(toolGroup.Id))
            {
                if(dispatchToolGroups.Count == 0) 
                    Sim.Delay(dispatchDelay, [() => { Dispatch(); }]);

                dispatchToolGroups.Add(toolGroup.Id);
            }
        }
        // Send Assigned Lots to Tool's Loadport

    }

    private void Dispatch()
    {
        for(int i = 0; i < dispatchToolGroups.Count; i++)
        {
            int id = dispatchToolGroups[i];
            ToolGroup toolGroup = ToolGroups[id];

            DispatchResult dr = dispatcher.Do(Sim.Now, toolGroup);
            foreach(var (tool, lots) in dr.Assignments)
            {
                foreach(var lot in lots)
                {
                    tool.AssignLot(lot);
                    toolGroup.LotQueue.Remove(lot);
                    Sim.Transport.Delivery(toolGroup.Location, tool, lot);
                }
            }

            if (dr.NextWakeup.HasValue && dr.NextWakeup.Value > Sim.Now)
            {
                int tgId = toolGroup.Id;
                Sim.DelayUntil(dr.NextWakeup.Value, [() => { ScheduleDispatch(tgId); }]);
            }
        }

        dispatchToolGroups.Clear();
    }

public long DISPATCH = 0;
    private void ScheduleDispatch(int toolGroupId)
    {
        DISPATCH++;
        if (dispatchToolGroups.Contains(toolGroupId)) return;
        if (dispatchToolGroups.Count == 0)
            Sim.Delay(dispatchDelay, [() => { Dispatch(); }]);
        dispatchToolGroups.Add(toolGroupId);
    }
}