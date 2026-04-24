namespace SharpSim;

public class Map()
{
    #region ObjectIds
    public int LastNodeId { get; set; }
    public int LastLinkId { get; set; }
    #endregion

    #region Properties
    public Dictionary<string, MapNode> Nodes { get; private set; } = new Dictionary<string, MapNode>();
    public Dictionary<string, MapLink> Links { get; private set; } = new Dictionary<string, MapLink>();
    #endregion

    public Map(Map other) : this()
    {
        Nodes = new Dictionary<string, MapNode>(other.Nodes);
        Links = new Dictionary<string, MapLink>(other.Links);
    }

    public virtual void Initialize()
    {
        LastNodeId = 0;
        LastLinkId = 0;

        Nodes = new Dictionary<string, MapNode>();
        Links = new Dictionary<string, MapLink>();
    }

    #region [Setter / Getter]
    public MapNode GetNode(string name)
    {
        return Nodes.Values.First(n => n.Name == name);
    }

    public MapNode GetNode(int id)
    {
        return Nodes.Values.First(n => n.Id == id);
    }

    public MapLink GetLink(MapNode fromNode, MapNode toNode)
    {
        try
        {
            foreach (var link in Links.Values)
            {
                if (link.FromNode == fromNode && link.ToNode == toNode)
                    return link;
            }
        }
        catch (System.Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        return null;
    }

    public MapLink GetLink(string fromId, string toId)
    {
        return this.GetLink(Nodes[fromId], Nodes[toId]);
    }
    #endregion

    #region [Add]
    public void AddMapNode(MapNode node)
    {
        if (Nodes.ContainsKey(node.Name))
            throw new Exception($"This graph has already contained node {node.Name}");

        Nodes.Add(node.Name, node);
    }

    public void AddMapLink(MapLink link)
    {
        if (Links.ContainsKey(link.Name))
            throw new Exception($"This graph has already contained link {link.Name}");

        Links.Add(link.Name, link);
        link.FromNode.OutLinks.Add(link);
        link.ToNode.InLinks.Add(link);
    }
    #endregion

    public virtual void RemoveMapNode(string nodeName)
    {
        if (Nodes.ContainsKey(nodeName))
        {
            var connLinks = Nodes[nodeName].InLinks.ToList();
            connLinks.AddRange(Nodes[nodeName].OutLinks);
            connLinks.ForEach(l => this.RemoveMapLink(l.Name));
            Nodes.Remove(nodeName);
        }
    }

    public virtual void RemoveMapLink(string linkName)
    {
        if (Links.ContainsKey(linkName))
        {
            var link = Links[linkName];
            link.FromNode.OutLinks.Remove(link);
            link.ToNode.InLinks.Remove(link);
            Links.Remove(linkName);
        }
    }
    #region [Other Methods]
    public void CheckBidirectionalLink()
    {
        foreach (MapLink link in Links.Values)
        {
            bool isBidirection = false;
            foreach (MapLink outLink in link.ToNode.OutLinks)
            {
                if (link.FromNode == outLink.ToNode)
                {
                    isBidirection = true;
                    break;
                }
            }

            link.SetBidirection(isBidirection);
        }
    }
    #endregion
}

