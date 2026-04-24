namespace SharpSim;
public class Arc
{
    public Vector2 Center { get; set; }
    public double Radius { get; set; }
    public Vector2 StartPos { get; set; }
    public Vector2 EndPos { get; set; }
    public DirectionType DirectionType { get; set; }
    public double StartAngle { get; set; }
    public double SweepAngle { get; set; }
    public double Length
    {
        get { return 2 * Math.PI * Radius * SweepAngle / 360; }
    }

    public Arc(Vector2 startPosition, Vector2 endPosition, Vector2 centerPoint)
    {
        this.StartPos = startPosition;
        this.EndPos = endPosition;
        this.Center = centerPoint;
        this.Radius = Vector2.Distance(startPosition, centerPoint);
        this.StartAngle = (StartPos - Center).AbsoluteAngleDegree();
        this.SweepAngle = Vector2.AbsoluteAngleDegree(StartPos - Center, EndPos - Center);
    }

    public Arc(Vector2 startPosition, Vector2 endPosition, double radius, DirectionType directionType = DirectionType.ClockWise)
    {
        this.StartPos = startPosition;
        this.EndPos = endPosition;
        this.Radius = radius;
        this.DirectionType = directionType;

        // Find Center Position
        bool isFound = false;
        var c1 = new Circle(this.StartPos, this.Radius);
        var c2 = new Circle(this.EndPos, this.Radius);
        var intersections = Circle.GetIntersectionPoints(c1, c2);

        foreach (var point in intersections)
        {
            var direction = Vector2.Direction(StartPos, EndPos, point);
            if (direction == this.DirectionType || direction == DirectionType.Colinear)
            {
                this.Center = point;
                isFound = true;
                break;
            }
        }

        if (isFound)
        {
            this.StartAngle = (StartPos - Center).AbsoluteAngleDegree();
            if(directionType == DirectionType.ClockWise)
                this.SweepAngle = Vector2.AbsoluteAngleDegree(EndPos - Center, StartPos - Center);
            else
                this.SweepAngle = Vector2.AbsoluteAngleDegree(StartPos - Center, EndPos - Center);
        }
        else
        { 
            Console.WriteLine("ERROR: Arc cannot generate; Cannot find center position");
        }
    }

    public bool IsInRange(double degree)
    {
        if (DirectionType == DirectionType.ClockWise)
        {
            var lowerLimit = this.StartAngle - this.SweepAngle;
            if (lowerLimit > 0)
            {
                if (this.StartAngle >= degree && degree >= lowerLimit)
                {
                    return true;
                }
            }
            else // lowLimit < 0
            {
                if (this.StartAngle >= degree && degree >= 0)
                {
                    return true;
                }
                else if (360 >= degree && degree >= 360 + lowerLimit)
                {
                    return true;
                }
            }
        }
        else if (DirectionType == DirectionType.CounterClockWise)
        {
            var higherLimit = this.StartAngle + this.SweepAngle;
            if (higherLimit < 360)
            {
                if (this.StartAngle <= degree && degree <= higherLimit)
                {
                    return true;
                }
            }
            else // higherLimit > 360
            {
                if (this.StartAngle <= degree && degree <= 360)
                {
                    return true;
                }
                else if (0 <= degree && degree <= higherLimit - 360)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public IntersectionType IsIntersectWith(LineSegment3D line, out List<Vector3> intersectionPoints)
    {
        return Arc.IsIntersectWith(this, line, out intersectionPoints);   
    }

    public static IntersectionType IsIntersectWith(Arc arc, LineSegment3D line, out List<Vector3> intersectionPoints)
    {
        intersectionPoints = new List<Vector3>();

        // line: aX + bY + c = 0
        var a = line.EndPoint.Y - line.StartPoint.Y;
        var b = line.StartPoint.X - line.EndPoint.X;
        var c = line.EndPoint.X * line.StartPoint.Y - line.StartPoint.X * line.EndPoint.Y;
        if (a < 0)
        {
            a = -a;
            b = -b;
            c = -c;
        }

        // t = p +/- q;
        // tan(p) = b/a
        // p = arctan(b/a)
        var p = Math.Atan(b / a);

        // cos(q) =  -(a * Xc + b * Yc + c)/ (R * Sqrt(a * a + b * b))
        // q = arccos(-(a * Xc + b * Yc + c)/ (R * Sqrt(a * a + b * b)))
        var temp = -(a * arc.Center.X + b * arc.Center.Y + c) / (arc.Radius * Math.Sqrt(a * a + b * b));
        var q = Math.Acos(temp);

        if (!double.IsNaN(q))
        {
            var t1 = Vector3.RotateByRadian(Vector3.UnitX, p + q, Vector3.Coordinate.Z);
            var t1Angle = t1.AbsoluteAngleDegree(Vector3.Coordinate.Z);
            var pt1 = new Vector3(arc.GetPointByDegree(t1Angle), 0);
            if (pt1.X >= 0 && pt1.Y >= 0 && line.IsOntheLine(pt1))
            {
                intersectionPoints.Add(pt1);
            }

            if (q != 0 && q != Math.PI)
            {
                var t2 = Vector3.RotateByRadian(Vector3.UnitX, p - q, Vector3.Coordinate.Z);
                var t2Angle = t2.AbsoluteAngleDegree(Vector3.Coordinate.Z);
                var pt2 = new Vector3(arc.GetPointByDegree(t2Angle), 0);
                if (pt2.X >= 0 && pt2.Y >= 0 && line.IsOntheLine(pt2))
                {
                    intersectionPoints.Add(pt2);
                }
            }
        }

        switch (intersectionPoints.Count)
        {
            case 0:
            default:
                return IntersectionType.None;
            case 1:
                return IntersectionType.One;
            case 2:
                return IntersectionType.Two;
        }
    }

    public Vector2 GetPointByOffset(double offset)
    {
        if (offset > this.Length) return EndPos;
        var theta = (360 * offset) / (2 * Math.PI * Radius);
        switch (this.DirectionType)
        {
            case DirectionType.ClockWise:
                return (StartPos - Center).RotateByDegree(360 - theta) + Center;
            case DirectionType.Colinear:
            case DirectionType.CounterClockWise:
                return (StartPos - Center).RotateByDegree(theta) + Center;
            default:
                return EndPos;
        }

    }

    public double GetOffsetByPoint(Vector2 pos)
    {
        var dist = Vector2.Distance(pos, this.Center);
        if (dist >= this.Radius - 0.001 && dist <= this.Radius + 0.001)
        {
            // TK
            var angle = (pos - Center).AbsoluteAngleDegree();
            var betweenAngle = System.Math.Abs(angle - StartAngle);
            if (IsInRange(angle))
            {
                return betweenAngle * System.Math.PI / 180 * Radius;
            }
        }

        return -1;
    }

    public Vector2 GetPointByDegree(double degree)
    {
        if (IsInRange(degree))
        {
            var rad = degree * Math.PI / 180;
            var pos = new Vector2(Math.Cos(rad), Math.Sin(rad));
            pos *= Radius;
            pos += Center;

            return new Vector2(Math.Round(pos.X, 3), Math.Round(pos.Y, 3));
        }
        else
        {
            return new Vector2(-1);
        }
    }
    
    public Vector2 DirectionOnArc(double offset)
    {
        Vector2 infront, behind;
        if (offset < 0.05)
        {
            infront = StartPos;
            behind = this.GetPointByOffset(offset + 0.025);
        }
        else if (offset > this.Length - 0.05)
        {
            infront = this.GetPointByOffset(offset-0.025);
            behind = EndPos;
        }
        else
        {
            infront = this.GetPointByOffset(offset - 0.05);
            behind = this.GetPointByOffset(offset + 0.05);
        }

        var direction = Vector2.Normalize(behind - infront);
        return direction;
    }

    public bool IsOntheArc(Vector2 pos)
    {
        var dist = Vector2.Distance(pos, this.Center);
        if (dist >= this.Radius - 0.001 && dist <= this.Radius + 0.001)
        {

            var angle = (pos - Center).AbsoluteAngleDegree();
            if (IsInRange(angle))
            {
                return true;
            }
            //if (angle >= StartAngle && angle <= StartAngle + SweepAngle)
            //{
            //    return true;
            //}
        }
        return false;
    }
}
