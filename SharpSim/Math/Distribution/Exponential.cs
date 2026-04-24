namespace SharpSim;
public class Exponential : Distribution
{
    public Exponential(double mean) : base(DistributionType.Exponential)
    {
        this.Mean = mean;
    }

    public override double GetNumber()
    {
        if (Mean <= 0) 
            throw new ArgumentException("Negative value is not allowed");
        double u = random.NextDouble();
        return (-Mean * Math.Log(u));
    }
}
