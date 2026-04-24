namespace SharpSim;

public class MapObject(int id, string name, Enum type = null)
{
    public int Id { get; private set; } = id;
    public string Name { get; private set; } = name;
    public Enum Type { get; private set; } = type;

    public virtual void Initialize() { }
    
    public override string ToString()
    {
        return this.Name;
    }
}
