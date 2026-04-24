using SharpSim;
using SharpSim;

namespace SharpSim.Demo;

public class Machine (Simulation<DemoHistory> sim, string name): SimNode<Simulation<DemoHistory>> (sim, name)
{
    public enum State
    {
        Idle,
        Processing
    }

    private Distribution processingTime = new Poisson(5); // Average processing time of 5 seconds
    public int ProcessingJobId { get; private set; } = -1;

    public override void Initialize()
    {
        base.Initialize();
        Console.WriteLine($"{this} initialized.");
        SetState(Simulation.Now, State.Idle);
    }

    public override void SetState(SimTime timeNow, Enum newState)
    {
        if(this.state is State machineState)
        {
            SimTime durationInCurrentState = timeNow - lastStateUpdatedTime;
            switch (machineState)
            {
                case State.Idle: // Update idle time in history
                    History.TotalIdleTime += durationInCurrentState.TotalSeconds;
                    break;
                case State.Processing: // Update processing time and count in history
                    History.TotalProcessingTime += durationInCurrentState.TotalSeconds;
                    break;
            }
        }
        base.SetState(timeNow, newState);
    }

    public double GetProcessingTime()
    {
        return 10;
        //return processingTime.GetNumber();
        //return new Random().NextDouble() * 5 + 1; // Random processing time between 1 and 6 seconds
    }

    public class ProcessJob(SimTime time, Machine machine, int jobId, List<Action>? callbacks) 
        : Event<Machine>(time, machine, callbacks)
    {
        public override void Execute()
        {
            Console.WriteLine($"{Node.Simulation.Now.TotalSeconds,-7} : {Node.Name} starts to process");
            double processingTime = Node.GetProcessingTime();
            Node.ProcessingJobId = jobId;
            Node.SetState(Node.Simulation.Now, State.Processing);
            Node.Simulation.Delay(processingTime, new List<Action> { ProcessFinished });
        }

        public void ProcessFinished()
        {
            Console.WriteLine($"{Node.Simulation.Now.TotalSeconds,-7} : {Node.Name} finishes processing job {Node.ProcessingJobId}");
            Node.SetState(Node.Simulation.Now, State.Idle);
            Callbacks.ForEach(action => action());
        }
    }
}
