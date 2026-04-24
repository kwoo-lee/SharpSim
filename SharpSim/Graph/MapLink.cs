namespace SharpSim;

public class MapLink(int id, string name, MapLinkType type, MapNode fromNode, MapNode toNode)
    : MapObject(id, name, type)
{
    public MapNode FromNode { get; private set; } = fromNode;
    public MapNode ToNode { get; private set; } = toNode;
    public Enum SubType { get; private set; }
    public List<MapLink> IntersectLinks { get; private set; } = new List<MapLink>();
    public object GeometryObj { get; private set; }
    public double Weight { get; private set; }
    public double Length { get; private set; }
    public double MaxSpeed { get; private set; }
    public bool IsBidirection { get; set; } = false;
    public bool IsSideWay { get; private set; }

    private Vector2 _designatedPose = new Vector2(0, 0);

    #region [Initialize]
    public override void Initialize()
    {
        base.Initialize();
        IsBidirection = false;
        _designatedPose = new Vector2(0, 0);
    }
    #endregion

    #region [Setter/Getter]
    public void SetGeometry(DirectionType direction, double radius)
    {
        switch ((MapLinkType)Type)
        {
            case MapLinkType.Curved:
                try
                {
                    if (direction == DirectionType.Colinear)
                        throw new Exception($"Can't generate curved link({this.Name}); with collinear direction");

                    var dist = Vector3.Distance(FromNode.Position, ToNode.Position);
                    if (dist <= 2 * radius)
                    {
                        var startPos = new Vector2(FromNode.Position.X, FromNode.Position.Y);
                        var endPos = new Vector2(ToNode.Position.X, ToNode.Position.Y);
                        var arc = new Arc(startPos, endPos, radius, direction);
                        GeometryObj = arc;
                        Length = arc.Length;
                    }
                    else
                        throw new Exception($"Can't generate curved link({this.Name}); Radius is too small.");
                }
                catch (Exception e)
                {
                    LogHandler.Error(e.Message.ToString());
                    // 오류시 Geometry를 선으로 정의한다.
                    goto SetGeometryAsStraight;
                }
                break;
            case MapLinkType.Straight:
                SetGeometryAsStraight:
                var line = new LineSegment3D(FromNode.Position, ToNode.Position);
                GeometryObj = line;
                Length = line.Length;
                break;
        }
    }

    public void SetBidirection(bool isBidirection) => IsBidirection = isBidirection;
    public void SetMaxSpeed(double maxSpeed) => MaxSpeed = maxSpeed;
    public virtual void SetWeight(double weight) => Weight = weight;
    public void SetLength(double length) => Length = length;
    public void SetSideWay(bool isSideWay) => IsSideWay = isSideWay;
    public void SetDesignatedPose(Vector2 pose) => _designatedPose = pose;
    public void SetSubType(Enum subType) => SubType = subType;
    #endregion

    #region [Other Methods]
    public virtual Vector3 GetPosition(double offset = 0)
    {
        try
        {
            switch ((MapLinkType)Type)
            {
                case MapLinkType.Straight:
                    var line = (LineSegment3D)GeometryObj;
                    return (line.Direction * offset) + FromNode.Position;
                case MapLinkType.Curved:
                    var arc = (Arc)GeometryObj;
                    return new Vector3(arc.GetPointByOffset(offset), FromNode.Position.Z);
                default:
                    throw new Exception($"Invalid Link Type{this.Name}");
            }
        }
        catch (Exception e)
        {
            LogHandler.Error(e.Message.ToString());
            return new Vector3(0, 0, 0);
        }
    }

    public Vector3 GetCenterPosition()
    {
        return GetPosition(Length / 2);
    }

    public virtual Vector3 GetDirection(double offset = 0)
    {
        try
        {
            if (Length > 0)
            {
                switch ((MapLinkType)Type)
                {
                    case MapLinkType.Straight:
                        var line = (LineSegment3D)GeometryObj;
                        return (line.Direction);
                    case MapLinkType.Curved:
                        var arc = (Arc)GeometryObj;
                        return new Vector3(arc.DirectionOnArc(offset), 0);
                    default:
                        throw new Exception($"Invalid Link Type{this.Name}");
                }
            }
            else
            {
                foreach (var inLink in FromNode.InLinks)
                {
                    if (inLink.Length == 0) continue;
                    return inLink.GetDirection(inLink.Length);
                }

                return Vector3.UnitX;
            }
        }
        catch (Exception e)
        {
            LogHandler.Error(e.Message.ToString());
            return new Vector3(0, 0, 0);
        }
    }

    public virtual double GetOffset(Vector3 pos)
    {
        try
        {
            switch ((MapLinkType)Type)
            {
                case MapLinkType.Straight:
                    var line = (LineSegment3D)GeometryObj;
                    return line.GetOffsetByPoint(pos);
                case MapLinkType.Curved:
                    var arc = (Arc)GeometryObj;
                    return arc.GetOffsetByPoint(new Vector2(pos.X, pos.Y));
                default:
                    throw new Exception($"Invalid Link Type{this.Name}");
            }
        }
        catch (Exception e)
        {
            LogHandler.Error(e.Message.ToString());
            return -1;
        }
    }

    public virtual Vector2 GetPose(double offset = 0)
    {
        if (_designatedPose.Length > 0)
        {
            return _designatedPose;
        }
        else
        {
            var direction = GetDirection(offset);
            var pose = new Vector2(direction.X, direction.Y);
            if (IsSideWay)
            {
                pose = Vector2.RotateByRadian(pose, System.Math.PI / 2);
            }

            return pose;
        }
    }

    public virtual Polygon GetPolygon(double offset)
    {
        Vector3 pos = this.GetPosition(offset);
        Vector2 pose = this.GetPose(offset);
        return SweepingVolume.FindVolumeByPose(new Vector2(pos.X, pos.Y), pose);
    }

    public double GetMaxX() => FromNode.Position.X >= ToNode.Position.X ? FromNode.Position.X : ToNode.Position.X;
    public double GetMinX() => FromNode.Position.X >= ToNode.Position.X ? ToNode.Position.X : FromNode.Position.X;
    public double GetMaxY() => FromNode.Position.Y >= ToNode.Position.Y ? FromNode.Position.Y : ToNode.Position.Y;
    public double GetMinY() => FromNode.Position.Y >= ToNode.Position.Y ? ToNode.Position.Y : FromNode.Position.Y;

    public bool IsOntheLink(Vector3 pos)
    {
        switch ((MapLinkType)this.Type)
        {
            case MapLinkType.Straight:
                var line = (LineSegment3D)this.GeometryObj;
                return line.IsOntheLine(pos);
            case MapLinkType.Curved:
                var arc = (Arc)this.GeometryObj;
                return arc.IsOntheArc(new Vector2(pos.X, pos.Y));
        }
        return false;
    }

    public bool IsIntersectWith(MapLink otherLink)
    {
        return IntersectLinks.Contains(otherLink);
    }

    public IntersectionType CheckIntersect(MapLink otherLink, out List<Vector3> crossingPoint)
    {
        var otherLinkType = (MapLinkType)otherLink.Type;
        switch ((MapLinkType)Type)
        {
            case MapLinkType.Straight:
                var lineSegment1 = (LineSegment3D)GeometryObj;
                if (otherLinkType == MapLinkType.Straight)
                {
                    var lineSegement2 = (LineSegment3D)otherLink.GeometryObj;
                    return lineSegment1.IsIntersectWith(lineSegement2, out crossingPoint);
                }
                else if (otherLinkType == MapLinkType.Curved)
                {
                    var arc1 = (Arc)otherLink.GeometryObj;
                    return arc1.IsIntersectWith(lineSegment1, out crossingPoint);
                }
                break;
            case MapLinkType.Curved:
                var arc2 = (Arc)GeometryObj;
                if (otherLinkType == MapLinkType.Straight)
                {
                    var lineSegement2 = (LineSegment3D)otherLink.GeometryObj;
                    return arc2.IsIntersectWith(lineSegement2, out crossingPoint);
                }
                else if (otherLinkType == MapLinkType.Curved)
                {
                    // Not Implemented
                }
                break;
        }

        crossingPoint = new List<Vector3>();
        return IntersectionType.None;
    }
    #endregion
}
