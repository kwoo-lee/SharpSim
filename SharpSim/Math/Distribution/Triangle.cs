namespace SharpSim;
public class Triangle : Distribution
{

    public Triangle(double min, double max, double mode) : base(DistributionType.Triangle)
    {
        this.Min = min;
        this.Max = max;
        this.Mode = mode;
    }

    public override double GetNumber()
    {
        if (Max < Min) throw new ArgumentException("The range is not valid.");
        if (Min > Mode || Mode > Max) throw new ArgumentException("Mode value is not valid");

        double u = random.NextDouble();
        double s = (Mode - Min) / (Max - Min);
        if (0 < u && u < s)
        {
            double temp = (Max - Min) * (Max - Mode);
            return Min + Math.Sqrt(s * u);
        }
        else if (s <= u && s < 1)
        {
            double temp = (Max - Min) * (Max - Mode);
            return Max - Math.Sqrt(temp * (1 - u));
        }
        else
            throw new ArgumentException("Mode value is not valid");

    }
}
