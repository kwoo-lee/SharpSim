namespace SharpSim;
public enum IntersectionType
{
    None,
    One, Two, Overlapped,
}

/// <summary>
///  3차원 공간에서의 선성분
/// </summary>
[Serializable]
public struct LineSegment3D : IEquatable<LineSegment3D>
{
    #region Public Fields
    public Vector3 StartPoint;
    public Vector3 EndPoint;
    public Vector3 CenterPoint;
    public double Inclination;
    public double InterceptX;
    public double InterceptY;
    public Vector3 Direction;
    public double Length;
    #endregion Public Fields

    public LineSegment3D(Vector3 point1, Vector3 point2)
    {
        this.StartPoint = point1;
        this.EndPoint = point2;
        this.CenterPoint = Vector3.Center(point1, point2);
        // y - point1.Y = (point2.Y - point1.Y) / (point2.X - v.X) * (x - point1.X)
        this.Inclination = (point2.Y - point1.Y) / (point2.X - point1.X);
        this.InterceptY = point1.Y - point1.X * this.Inclination;
        this.InterceptX = point1.X - point1.Y / this.Inclination;
        this.Direction = Vector3.Direction(point1, point2);
        this.Length = Vector3.Distance(point1, point2);
    }

    public static bool InGrid(LineSegment3D line, Vector3 point)
    {
        double minX = Math.Min(line.StartPoint.X, line.EndPoint.X);
        double maxX = Math.Max(line.StartPoint.X, line.EndPoint.X);
        if (minX <= point.X && point.X <= maxX)
        {
            double minY = Math.Min(line.StartPoint.Y, line.EndPoint.Y);
            double maxY = Math.Max(line.StartPoint.Y, line.EndPoint.Y);
            if (minY <= point.Y && point.Y <= maxY)
            {
                return true;
            }
        }
        return false;
    }

    public bool IsOntheLine(Vector3 point)
    {
        if (LineSegment3D.InGrid(this, point))
        {
            var distToStart = Vector3.Distance(point, StartPoint);
            var distToEnd = Vector3.Distance(point, EndPoint);
            var distStartToEnd = Vector3.Distance(StartPoint, EndPoint);
            var onTheLine = distToStart + distToEnd - distStartToEnd;
            if (-1 < onTheLine && onTheLine < 1)
                return true;
            else
                return false;
        }
        return false;
    }

    public double GetOffsetByPoint(Vector3 point)
    {
        if (IsOntheLine(point))
        {
            return Vector3.Distance(StartPoint, point);
        }
        return -1;
    }
    /// <summary>
    /// 일단 직선과
    /// </summary>
    /// <param name="arc"></param>
    /// <returns></returns>
    public Vector2 GetIntersectionPoint(Arc arc)
    {
        var center = arc.Center;
        var dist = PerpendicularDistance(this, center);

        if (dist <= arc.Radius)
        {
            //      a == Inclination & 
            //      b = -1 & 
            //      c = InterceptY
            //* Perpendicular Point X_i = (X, y)
            //      x = (b*b*x_p - a*b*y_p - a*c) / (a*a + b*b)
            //      y = (a*a*y_p - a*b*x_p - b_c) / (a*a + b*b)
            var x = (center.X + Inclination * center.Y - Inclination * InterceptY) / (Inclination * Inclination + 1);
            var y = (Inclination * Inclination * center.Y + InterceptY + Inclination * center.X) / (Inclination * Inclination + 1);
            var perpendicularPoint = new Vector2(x, y);
            if (dist == arc.Radius)
            {
                return perpendicularPoint;
            }
            else if (dist < arc.Radius)
            {
                var direction2D = Vector2.Normalize(new Vector2(EndPoint - StartPoint, Vector3.Plane.XY));
                var h = Math.Sqrt(arc.Radius * arc.Radius - dist * dist);
                var ip1 = perpendicularPoint + h * direction2D;
                var ip2 = perpendicularPoint - h * direction2D;

                if (arc.IsOntheArc(ip1))
                {
                    if (arc.IsOntheArc(ip2))
                    {
                        Console.WriteLine("ERROR : There are 2 intersection points");
                    }
                    else
                    {
                        return ip1;
                    }
                }
                else if (arc.IsOntheArc(ip2))
                {
                    return ip2;
                }
            }
        }

        return new Vector2(-1, -1);
    }
    
    public IntersectionType IsIntersectWith(LineSegment3D otherLine, out List<Vector3> intersectionPoints)
    {
        var type = IsIntersectWith(this, otherLine, out intersectionPoints);
        return type;
    }

    public static IntersectionType IsIntersectWith(LineSegment3D line1, LineSegment3D line2, out List<Vector3> intersectionPoints)
    {
        var points = new List<Vector3>();
        var r = line1.EndPoint - line1.StartPoint;
        var s = line2.EndPoint - line2.StartPoint;
        var rsCross = Vector3.Cross(r, s, Vector3.Coordinate.Z);
   
        if (Math.Abs(rsCross) < double.Epsilon)
        {
            Action<LineSegment3D, Vector3> CollinearCheck = (line, point) =>
            {
                var orientation = Orientation(line, point);
                if (orientation == 0 && InGrid(line, point))
                {
                    if (!points.Contains(point))
                        points.Add(point);
                }    
            };

            CollinearCheck(line2, line1.StartPoint);
            CollinearCheck(line2, line1.EndPoint);
            CollinearCheck(line1, line2.StartPoint);
            CollinearCheck(line1, line2.EndPoint);
        }
        else
        {
            var t = ((line2.StartPoint - line1.StartPoint).X * s.Y - (line2.StartPoint - line1.StartPoint).Y * s.X) / rsCross;//(q - p).Cross(s) / rxs;
            var u = ((line2.StartPoint - line1.StartPoint).X * r.Y - (line2.StartPoint - line1.StartPoint).Y * r.X) / rsCross;//(q - p).Cross(r) / rxs;
            if (0 <= t && t <= 1 && 0 <= u && u <= 1) //두직선이 한점에서 만난다.
            {
                //Console.WriteLine(this.StartPoint + t * r);
                points.Add(line1.StartPoint + t * r);
            }
        }

        intersectionPoints = points;
        if (points.Count >= 2)
            return IntersectionType.Overlapped;
        else if (points.Count == 1)
            return IntersectionType.One;
        else 
            return IntersectionType.None;
    }

    public static IntersectionType IsIntersectWith(LineSegment3D line, Arc arc, out List<Vector3> intersectionPoints)
    {
        return arc.IsIntersectWith(line, out intersectionPoints);
    }

    /// <summary>
    ///  Determine the direction of the point from the line
    ///  Positive : ClockWise(right side)
    ///  Negative : CounterClockWise(left side)
    /// </summary>
    /// <param name="line">standard line</param>
    /// <param name="pos">target point</param>
    /// <returns>
    /// </returns>
    public static double Orientation(LineSegment3D line, Vector3 pos)
    {
        return Math.Round(Vector3.Cross(pos - line.StartPoint, line.EndPoint - line.StartPoint, Vector3.Coordinate.Z), 2);
    }
        

    public static double PerpendicularDistance(LineSegment3D line, Vector2 point)
    {
        // let the line equation is a*x + b*y + c = 0 and
        // a point is (x_p, y_p)
        // dist = abs(a*x_p + b*y_p + c) / sqrt(a*a + b*b)
        //      a == Inclination & 
        //      b = -1 & 
        //      c = InterceptY
        return Math.Abs(line.Inclination * point.X - 1 * point.Y + line.InterceptY) / Math.Sqrt(line.Inclination * line.Inclination + 1);
    }

    public bool Equals(LineSegment3D other)
    {
        throw new NotImplementedException();
    }
}
 
