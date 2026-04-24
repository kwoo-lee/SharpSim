
namespace SharpSim;

public class MapNode(int id, string name, Vector3 position) : MapObject(id, name)
{
    public Vector3 Position { get; private set; } = position;
    public List<MapLink> InLinks { get; private set; } = new List<MapLink>();
    public List<MapLink> OutLinks { get; private set; } = new List<MapLink>();

    public override void Initialize()
    {
        base.Initialize();
        InLinks = new List<MapLink>();
        OutLinks = new List<MapLink>();
    }
}