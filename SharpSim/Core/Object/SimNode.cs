namespace SharpSim;

public interface ISimNode
{
    int Id { get; }
    string Name { get; }
    void SetState(Enum newState);
    void Initialize();      
}

public class SimNode<TSimulation, THistory> : SimObject, ISimNode 
    where TSimulation : ISimulation 
    where THistory : IHistory 
{
    protected readonly TSimulation Sim;
    protected readonly THistory History;

    protected Enum? state;
    protected SimTime lastStateUpdatedTime;

    public List<SimObject> Entities { get; private set; } = new List<SimObject>();
    public Location? Location { get; protected set; } = null;

    public SimNode(TSimulation simulation, THistory history, int id, string name) : base(id, name)
    {
        Sim = simulation;
        History = history;
        Sim.AddNode(this);
    }

    public virtual void Initialize()
    {
        lastStateUpdatedTime = 0;
        Entities.Clear();
    }

    public virtual void SetState(Enum newState)
    {
        this.state = newState;
        lastStateUpdatedTime = Sim.Now;
    }
}


