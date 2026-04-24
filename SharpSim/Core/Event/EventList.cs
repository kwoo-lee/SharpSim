namespace SharpSim;

public interface IEventList
{
    public int Count {get;}
    public void Add(IEvent evt);
    public IEvent? RetrieveNext();
    public void Remove(IEvent evt);
}


public class EventList : IEventList
{
    private List<IEvent> events = new List<IEvent>();

    public int Count => events.Count;

    public void Add(IEvent evt)
    {
        int index = events.BinarySearch(evt, new EventComparer());
        if(index < 0) index = ~index;
        events.Insert(index, evt);
    }

    public IEvent? RetrieveNext()
    {
        if (events.Count == 0) return null;
        IEvent nextEvent = events[0];
        events.RemoveAt(0);
        return nextEvent;
    }

    public void Remove(IEvent evt)
    {
        events.Remove(evt);
    }
}

public class EventComparer : IComparer<IEvent>  
{
    public int Compare(IEvent? x, IEvent? y)
    {
        if (x == null && y == null) return 0;
        if (x == null) return -1;
        if (y == null) return 1;

        return x.Time.CompareTo(y.Time); // Assuming IEvent has a Time property
    }
}
