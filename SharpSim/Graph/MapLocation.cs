namespace SharpSim;
public struct Location(MapNode node, MapLink link, double offset)
{
    #region [Static]
    public static double Window { get; private set; } = 100;
    public static void SetWindow(double window) => Window = window;
    #endregion

    public MapNode Node { get; private set; } = node;
    public MapLink Link { get; private set; } = link;
    public double Offset { get; set; } = offset;

    public Location(MapNode node) : this(node, null, -1.0) { }
    public Location(MapLink link, double offset) : this(null, link, offset) { }

    public Vector3 GetPosition()
    {
        if (Node is null)
            return Link.GetPosition(Offset);
        else
            return Node.Position;
    }

    public Vector3 GetDirection()
    {
        if (Node is null)
            return Link.GetDirection(Offset);
        else
            return new Vector3(1, 0, 0);
    }

    public override string ToString()
    {
        if (Node is null && Link is null)
            return "";
        else if (Node is null)
            return Link.Name + "/" + Offset.ToString("F");
        else
            return Node.Name;
    }

    public static bool operator ==(Location location, MapNode node)
    {
        if (location.Node is null)
        {
            if (node is null)
                return location.Link == null;
            else
                return (location.Link.FromNode == node && location.Offset <= Window) ||
                (location.Link.ToNode == node && location.Offset >= location.Link.Length - Window);
        }
        else
        {
            return location.Node == node;
        }
    }

    public static bool operator !=(Location location, MapNode node) => !(location == node);
    public static bool operator ==(MapNode node, Location location) => location == node;
    public static bool operator !=(MapNode node, Location location) => !(location == node);

    public static bool operator ==(Location location1, Location location2)
    {
        if (location1.Node is null)
        {
            if (location2.Node is null)
            {
                // Link Vs. Link
                // Case1) Same Link Case
                // Abs(offset1 - offset2) <= 100
                // Case2-1) Other Link Case1
                // link1.ToNode.OutLinks.Contains(link2) &&
                // (link1.Length - offset1) + offset2 <= 100
                // Case2-2) Ohter Link Case2
                // link1.FromNode.InLinks.Contains(link2) &&
                // offset1 + (link2.Length - offset2) <= 100
                // Case3) Else --> False

                if (location1.Link == location2.Link)
                {
                    return System.Math.Abs(location1.Offset - location2.Offset) <= Window;
                }
                else
                {
                    if (location1.Link.ToNode.OutLinks.Contains(location2.Link))
                        return (location1.Link.Length - location1.Offset) + location2.Offset <= Window;
                    else if (location1.Link.FromNode.InLinks.Contains(location2.Link))
                        return location1.Offset + (location2.Link.Length - location2.Offset) <= Window;
                    else
                        return false;
                }
            }
            else
            {
                // Link Vs. Node
                return location1 == location2.Node;
            }
        }
        else
        {
            if (location2.Node is null)
                // Node vs. Link
                return location1.Node == location2;
            else
                // Node vs. Node
                return location1.Node == location2.Node;
        }
    }

    public static bool operator !=(Location location1, Location location2) => !(location1 == location2);
}
