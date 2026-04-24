using SharpSim;

namespace SharpSim.Demo;

public class Source : SimNode<Simulation<DemoHistory>>
{
    private SimTime lastQueueUpdateTime = new SimTime(0);
    private int lastJobId = 0;
    private List<Machine> machines;
    private Queue<int> jobQueue = new Queue<int>();

    public Source(Simulation<DemoHistory> sim, string name, List<Machine> machines) : base(sim, name)
    {
        this.machines = machines;
    }

    public override void Initialize()
    {
        LogHandler.Info($"{this} initialized.");
        Simulation.Schedule(new GenerateJob(Simulation.Now, this));
    }

    public class GenerateJob(SimTime time, Source source) : Event<Source>(time, source)
    {
        public override void Execute()
        {
            LogHandler.Info($"{Time.TotalSeconds,-7} : {Node.Name} generates a job");
            Node.Simulation.Schedule(new GenerateJob(Time + 5, Node)); // Schedule next job generation in 10 seconds
            
            int jobId = ++Node.lastJobId;
            var machine = Node.machines.FirstOrDefault(m => m.State is Machine.State.Idle);

            // Update waiting time in queue before assigning job to machine or enqueueing   
            SimTime waitTime = Node.Simulation.Now - Node.lastQueueUpdateTime;
            Node.Simulation.History.TotalWaitingTimeInQueue += waitTime.TotalSeconds;
            Node.lastQueueUpdateTime = Node.Simulation.Now;

            if (machine != null)
            {
                Console.WriteLine($"{Time.TotalSeconds,-7} : {Node.Name} assigns job {jobId} to {machine.Name}");
                Node.Simulation.Schedule(new Machine.ProcessJob(Node.Simulation.Now, machine, jobId, new List<Action> { MachineProcessFinished }));
            }
            else
            {
                Console.WriteLine($"{Time.TotalSeconds,-7} : {Node.Name} no available machines for job {jobId}");
                Node.jobQueue.Enqueue(jobId);
            }
        }

        public void MachineProcessFinished()
        {
            // This callback can be used to trigger actions when a machine finishes processing a job
            if(Node.jobQueue.Count>0)
            {
                int nextJobId = Node.jobQueue.Dequeue();
                var machine = Node.machines.FirstOrDefault(m => !m.IsBusy);

                if (machine != null)
                {
                    Console.WriteLine($"{Node.Simulation.Now.TotalSeconds,-7} : {Node.Name} assigns job {nextJobId} to {machine.Name}");
                    Node.Simulation.Schedule(new Machine.ProcessJob(Node.Simulation.Now, machine, nextJobId, new List<Action> { MachineProcessFinished }));
                }
            }
        }
    }
}
