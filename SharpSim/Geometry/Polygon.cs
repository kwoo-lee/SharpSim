namespace SharpSim;
public class Polygon : IEquatable<Polygon>
{
    private Vector2[] _vertices;
    private double _minX;
    private double _minY;
    private double _maxX;
    private double _maxY;

    public Vector2[] Vertices => _vertices;

    public double MinX => _minX;

    public double MinY => _minY;

    public double MaxX => _maxX;

    public double MaxY => _maxY;

    public Polygon(List<Vector2> points)
    {
        _vertices = RunJarvisMarch(points);
        this.CalculateMinMax();
    }

    private void CalculateMinMax()
    {
        _minX = double.MaxValue;
        _minY = double.MaxValue;
        _maxX = double.MinValue;
        _maxY = double.MinValue;

        for (int i = 0; i < _vertices.Length; i++)
        {
            var vertice = _vertices[i];
            if (_minX > vertice.X) _minX = vertice.X;

            if (_minY > vertice.Y) _minY = vertice.Y;

            if (_maxX < vertice.X) _maxX = vertice.X;

            if (_maxY < vertice.Y) _maxY = vertice.Y;
        }
    }

    public bool IsIntersect(Polygon otherPolygon, bool bool1)
    {

        return true;
    }

    public bool IsIntersect(Polygon otherPolygon)
    {
        if (otherPolygon.MinX > this.MaxX) return false;
        if (otherPolygon.MinY > this.MaxY) return false;
        if (otherPolygon.MaxX < this.MinX) return false;
        if (otherPolygon.MaxY < this.MinY) return false;

        foreach (var othersVertice in otherPolygon.Vertices)
        {
            if (this.IsInPolygon(othersVertice))
                return true;
        }

        for (int i = 0; i < _vertices.Length; i++)
        {
            var fromPt = _vertices[i];
            var toPt = i < _vertices.Length - 1 ? _vertices[i + 1] : _vertices[0];
            var line = new LineSegment2D(fromPt, toPt);

            for (int j = 0; j < otherPolygon.Vertices.Length; j++)
            {
                var otherFromPt = otherPolygon.Vertices[j];
                var otherToPt = j < otherPolygon.Vertices.Length - 1
                    ? otherPolygon.Vertices[j + 1]
                    : otherPolygon.Vertices[0];
                var otherLine = new LineSegment2D(otherFromPt, otherToPt);

                var intersectionType = line.IsIntersectWith(otherLine, out List<Vector2> pts);
                if (intersectionType != IntersectionType.None)
                    return true;
            }

        }

        return false;
    }

    public static bool IsIntersect(Polygon value1, Polygon value2)
    {
        bool isInPolygon = false;

        foreach (var othersVertice in value2.Vertices)
        {
            if (value1.IsInPolygon(othersVertice))
                isInPolygon = true;
        }

        return isInPolygon;
    }

    public static Vector2[] RunJarvisMarch(List<Vector2> points)
    {
        // Find the left most point for starting
        Vector2 start = points[0];
        for (int i = 1; i < points.Count; i++)
        {   
            if (points[i].X < start.X)
                start = points[i];
        }

        var result = new List<Vector2>() { start } ; //set is used to avoid entry of duplicate points
        var collinearPoints = new List<Vector2>();

        Vector2 current = start;
        while (true)
        {
            Vector2 nextTarget = points[0];
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i] == current) //when selected point is current point, ignore rest part
                    continue;

                DirectionType direction = Vector2.Direction(nextTarget, points[i], current);
                if (direction == DirectionType.ClockWise)
                {   //when ith point is on the left side
                    nextTarget = points[i];
                    collinearPoints = new List<Vector2>();
                }
                else if (direction == DirectionType.Colinear) //if three points are collinear
                {
                    var distToNextTarget = Vector2.DistanceSquared(current, nextTarget);
                    var distToithPoint = Vector2.DistanceSquared(current, points[i]);
                    //Add closer one to collinear list
                    if (distToNextTarget < distToithPoint)
                    {
                        collinearPoints.Add(nextTarget);
                        nextTarget = points[i];
                    }
                    else //when ith point is closer or same as nextTarget
                    {
                        collinearPoints.Add(points[i]);
                    }
                }
            }

            foreach (Vector2 pt in collinearPoints)
            {
                if (!collinearPoints.Contains(pt)) //avoid entry of duplicate points
                {
                    result.Add(pt);
                }
            }

            if (nextTarget == start) //when next point is start it means, the area covered
                break;

            result.Add(nextTarget);
            current = nextTarget;
        }

        return result.ToArray();
    }

    public static bool IsInPolygon(Vector2[] polygon, Vector2 targetPt)
    {
        int num = 0;

        for (int i = 0; i < polygon.Length; i++)
        {
            Vector2 fromPt = polygon[i];
            Vector2 toPt = polygon[(i + 1) % polygon.Length];

            var line = new LineSegment2D(fromPt, toPt);
            if (line.IsOntheLine(targetPt))
                return true;

            var ray = new LineSegment2D(targetPt, new Vector2(Math.Max(fromPt.X, toPt.X) + 1, targetPt.Y));
            var inter = LineSegment2D.IsIntersectWith(line, ray, out List<Vector2> asd);
            if (LineSegment2D.IsIntersectWith(line, ray, out List<Vector2> intersections) == IntersectionType.One)
                num++;

        }

        return num % 2 == 1;
    }

    public bool IsInPolygon(Vector2 targetPt)
    {
        if (_maxX < targetPt.X) return false;
        if (_minX > targetPt.X) return false;
        if (_maxY < targetPt.Y) return false;
        if (_minY > targetPt.Y) return false;
        return Polygon.IsInPolygon(_vertices, targetPt);
    }

    public void RotateByDegree(Vector2 centerPos, double degree)
    {
        RotateByRadian(centerPos, degree * Math.PI / 180);
    }

    public void RotateByRadian(Vector2 centerPos, double radian)
    {
        var newPoints = new List<Vector2>();
        for (int i = 0; i < _vertices.Length; i++)
        {
            var v = _vertices[i];
            var rot = (v - centerPos).RotateByRadian(radian);
            newPoints.Add(centerPos + rot);
        }

        _vertices = newPoints.ToArray();
        this.CalculateMinMax();
    }

    public static Polygon RotateByDegree(Polygon polygon, Vector2 centerPos, double degree)
    {
        return RotateByRadian(polygon, centerPos, degree * Math.PI / 180);
    }

    public static Polygon RotateByRadian(Polygon polygon, Vector2 centerPos, double radian)
    {
        var newPoints = new List<Vector2>();
        for (int i = 0; i < polygon.Vertices.Length; i++)
        {
            var v = polygon.Vertices[i];
            var rot = (v - centerPos).RotateByRadian(radian);
            newPoints.Add(centerPos + rot);
        }

        return new Polygon(newPoints);
    }

    public bool Equals(Polygon other)
    {
        if (_vertices.Length == other.Vertices.Length)
        {
            foreach (var vertex in _vertices)
            {
                bool contained = false;
                foreach (var otherVertex in other.Vertices)
                {
                    if (otherVertex == vertex)
                    {
                        contained = true;
                        break;
                    }
                }

                if (!contained)
                    return false;
            }

            return true;
        }
        else
        {
            return false;
        }
    }
}
