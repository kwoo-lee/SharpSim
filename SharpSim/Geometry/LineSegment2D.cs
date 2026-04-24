using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpSim
{
    [Serializable]
    public struct LineSegment2D : IEquatable<LineSegment2D>
    {
        public enum LineSegmentType
        {
            Normal,
            ParallelX,
            ParallelY,
        }
        #region Public Fields
        private LineSegmentType _type;
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public Vector2 CenterPoint;
        public double Inclination;
        public double InterceptX;
        public double InterceptY;
        public Vector2 Direction;
        public double Length;
        #endregion Public Fields

        public LineSegment2D(Vector2 point1, Vector2 point2)
        {
            this.StartPoint = point1;
            this.EndPoint = point2;
            this.CenterPoint = (point1 + point2) / 2; ;
            // y - point1.Y = (point2.Y - point1.Y) / (point2.X - v.X) * (x - point1.X)

            if (point1.X == point2.X)
            {
                _type = LineSegmentType.ParallelY;
                this.Inclination = double.NaN;
                this.InterceptX = point1.X;
                this.InterceptY = double.NaN;
            }
            else if (point1.Y == point2.Y)
            {
                _type = LineSegmentType.ParallelX;
                this.Inclination = 0;
                this.InterceptX = double.NaN;
                this.InterceptY = point1.Y;
            }
            else
            {
                _type = LineSegmentType.Normal;
                this.Inclination = (point2.Y - point1.Y) / (point2.X - point1.X);
                this.InterceptY = point1.Y - point1.X * this.Inclination;
                this.InterceptX = point1.X - point1.Y / this.Inclination;
            }

            this.Direction = Vector2.Normalize(point2 - point1);
            this.Length = Vector2.Distance(point1, point2);
        }

        public bool IsInGrid(Vector2 pt)
        {
            return IsInGrid(this, pt);
        }

        public static bool IsInGrid(LineSegment2D line, Vector2 point)
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

        public bool IsOntheLine(Vector2 point)
        {
            if (LineSegment2D.IsInGrid(this, point))
            {
                var distToStart = Vector2.Distance(point, StartPoint);
                var distToEnd = Vector2.Distance(point, EndPoint);
                var distStartToEnd = Vector2.Distance(StartPoint, EndPoint);
                var onTheLine = distToStart + distToEnd - distStartToEnd;
                if (-Double.Epsilon < onTheLine && onTheLine < Double.Epsilon)
                    return true;
                else
                    return false;
            }
            return false;
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
                    var h = Math.Sqrt(arc.Radius * arc.Radius - dist * dist);
                    var ip1 = perpendicularPoint + h * Direction;
                    var ip2 = perpendicularPoint - h * Direction;

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

        public bool IsOnTheLine(Vector2 point)
        {
            return IsOnTheLine(this, point);
        }

        public static bool IsOnTheLine(LineSegment2D line, Vector2 point)
        {
            return IsCollinearWith(line, point) && IsInGrid(line, point);
        }

        public bool IsCollinearWith(Vector2 point)
        {
            return IsCollinearWith(this, point);
        }

        public Vector2 GetPerpencularPoint(Vector2 point)
        {
            // a *x + b *y + c= 0;
            // Inclination * x - 1 * y + InterceptY = 0;\
            Vector2 pp = new Vector2(double.NaN, double.NaN);
            if (_type == LineSegmentType.ParallelX)
            {
                pp = new Vector2(point.X, this.InterceptY);
            }
            else if (_type == LineSegmentType.ParallelY)
            {
                pp = new Vector2(this.InterceptX, point.Y);
            }
            else
            {
                double a = Inclination;
                double b = -1;
                double c = InterceptY;

                double k = -(a * point.X + b * point.Y + c) / (a * a + b * b);
                double x2 = point.X + a * k;
                double y2 = point.Y + b * k;

                double test = a * x2 + b * y2 + c;

                pp = new Vector2(x2, y2);
            }

            if (this.IsOnTheLine(pp))
                return pp;
            else
                return new Vector2(double.NaN, double.NaN);
        }

        public static bool IsCollinearWith(LineSegment2D line, Vector2 point)
        {
            var direction = Vector2.Direction(line.StartPoint, line.EndPoint, point);
            return direction == DirectionType.Colinear;
        }

        public IntersectionType IsIntersectWith(LineSegment2D otherLine, out List<Vector2> intersectionPoints)
        {
            var type = IsIntersectWith(this, otherLine, out intersectionPoints);
            return type;
        }

        public static IntersectionType IsIntersectWith(LineSegment2D line1, LineSegment2D line2, out List<Vector2> intersectionPoints)
        {
            var points = new List<Vector2>();
            var r = line1.EndPoint - line1.StartPoint;
            var s = line2.EndPoint - line2.StartPoint;
            var rsCross = Vector2.CrossProduct(r, s);

            if (Math.Abs(rsCross) < double.Epsilon)
            {
                if (IsOnTheLine(line2, line1.StartPoint) && !points.Contains(line1.StartPoint))
                    points.Add(line1.StartPoint);

                if (IsOnTheLine(line2, line1.EndPoint) && !points.Contains(line1.EndPoint))
                    points.Add(line1.EndPoint);

                if (IsOnTheLine(line1, line2.StartPoint) && !points.Contains(line2.StartPoint))
                    points.Add(line2.StartPoint);

                if (IsOnTheLine(line1, line2.EndPoint) && !points.Contains(line2.EndPoint))
                    points.Add(line2.EndPoint);
            }
            else
            {
                var t = ((line2.StartPoint - line1.StartPoint).X * s.Y - (line2.StartPoint - line1.StartPoint).Y * s.X) / rsCross;//(q - p).Cross(s) / rxs;
                var u = ((line2.StartPoint - line1.StartPoint).X * r.Y - (line2.StartPoint - line1.StartPoint).Y * r.X) / rsCross;//(q - p).Cross(r) / rxs;
                if (0 <= t && t <= 1 && 0 <= u && u <= 1) //두직선이 한점에서 만난다.
                {
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

        public static double PerpendicularDistance(LineSegment2D line, Vector2 point)
        {
            if (line._type == LineSegmentType.ParallelX)
            {
                return Math.Abs(point.Y - line.InterceptY);
            }
            else if (line._type == LineSegmentType.ParallelY)
            {
                return Math.Abs(point.X - line.InterceptX);
            }
            else
            {
                // let the line equation is a*x + b*y + c = 0 and
                // a point is (x_p, y_p)
                // dist = abs(a*x_p + b*y_p + c) / sqrt(a*a + b*b)
                //      a == Inclination & 
                //      b = -1 & 
                //      c = InterceptY
                return Math.Abs(line.Inclination * point.X - 1 * point.Y + line.InterceptY) / Math.Sqrt(line.Inclination * line.Inclination + 1);
            }
        }

        public bool Equals(LineSegment2D other)
        {
            throw new NotImplementedException();
        }
    }
}
