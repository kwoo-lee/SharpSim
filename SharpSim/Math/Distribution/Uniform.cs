namespace SharpSim;
public class Uniform : Distribution
{
    public Uniform(double min, double max) : base(DistributionType.Uniform)
    {
        this.Min = min;
        this.Max = max;
        this.Mean = (max - min) / 2;
    }

    public override double GetNumber()
    {
        if (Max < Min) throw new Exception("The range is not valid.");
        double u = random.NextDouble();
        return (Min + (Max - Min) * u);
    }
}
