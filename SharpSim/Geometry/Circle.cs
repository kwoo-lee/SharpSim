namespace SharpSim;
public class Circle
{
    public Vector2 Center { get; set; }
    public double Radius { get; set; }
    public double X => Center.X;
    public double Y => Center.Y;

    public Circle(double x, double y, double radius)
    {
        this.Center = new Vector2(x, y);
        this.Radius = radius;
    }

    public Circle(Vector2 center, double radius)
    {
        this.Center = center;
        this.Radius = radius;
    }

    public double DistanceSquared(Circle otherCircle)
    {
        double dx = this.X - otherCircle.X;
        double dy = this.Y - otherCircle.Y;
        return (dx * dx) + (dy * dy);
    }

    public double Distance(Circle otherCircle)
    {
        return Math.Sqrt(this.DistanceSquared(otherCircle));
    }

    public static List<Vector2> GetIntersectionPoints(Circle c1, Circle c2)
    {
        var intersections = new List<Vector2>();

        // Find the distance between the centers.
        double dist = c1.Distance(c2);

        // See how many solutions there are.
        if (dist > c1.Radius + c2.Radius)
        { } // No solutions, the circles are too far apart.
        else if (dist < Math.Abs(c1.Radius - c2.Radius))
        { }// No solutions, one circle contains the other.
        else if ((dist == 0) && (c1.Radius == c2.Radius))
        { }// No solutions, the circles coincide.
        else
        {
            // Find a and h.
            double a = (c1.Radius * c1.Radius - c2.Radius * c2.Radius + dist * dist) / (2 * dist);
            double h = Math.Sqrt(c1.Radius * c1.Radius - a * a);

            // Find P2.
            var p2 = new Vector2(c1.X + a * (c2.X - c1.X) / dist,
                                c1.Y + a * (c2.Y - c1.Y) / dist);
            var t = new Vector2(c1.X, c1.Y) + a * new Vector2(c2.X - c1.X, c2.Y - c1.Y) / dist;
            // Get the points P3.
            intersections.Add(new Vector2(
                (p2.X + h * (c2.Y - c1.Y) / dist),
                (p2.Y - h * (c2.X - c1.X) / dist)));

            if (dist != c1.Radius + c2.Radius)
            { // P4
                var p3 = new Vector2(
                (p2.X - h * (c2.Y - c1.Y) / dist),
                (p2.Y + h * (c2.X - c1.X) / dist));
                intersections.Add(p3);
            }

            for (int i = 0; i < intersections.Count; i++)
            {
                var testDist = Vector2.Distance(c1.Center, intersections[i]);
                if (c1.Radius != testDist)
                    ;
            }
        }

        return intersections;
    }
}
