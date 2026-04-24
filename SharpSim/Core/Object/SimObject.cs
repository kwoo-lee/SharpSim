namespace SharpSim;


public abstract class SimObject(int id, string name = "") 
{
    public int Id { get; private set; } = id;
    public string Name { get; private set; } = name; 
    public override string ToString() => $"[{GetType().Name}] Id: {Id}, Name: {Name}";
}
