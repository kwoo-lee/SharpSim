namespace SharpSim;

public interface IEvent
{
    SimTime Time { get; }
    void Execute();
    List<Action> Callbacks { get; }
}

// 단발성 이벤트 (기존 방식)
public abstract class Event<TNode>(SimTime time, TNode node, List<Action>? callbacks = null) 
: IEvent where TNode : ISimNode
{
    public SimTime Time { get; private set; } = time;
    public TNode Node { get; private set; } = node;
    public List<Action> Callbacks { get; private set; } = callbacks ?? new List<Action>();
    public abstract void Execute();
}

public class TimeDelayEvent(SimTime time, List<Action>? callbacks = null) : IEvent 
{
    public SimTime Time { get; } = time;
    public List<Action> Callbacks { get; private set; } = callbacks ?? new List<Action>();  

    public void Execute() 
    { 
        Callbacks.ForEach(action => action());
    }
}
