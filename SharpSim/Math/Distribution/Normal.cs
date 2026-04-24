namespace SharpSim;
public class NormalDist : Distribution
{
    public NormalDist(double mean, double std) : base(DistributionType.Normal)
    {
        this.Mean = mean;
        this.Std = std;
    }

    public override double GetNumber()
    {
        // Parameter 
        double u = random.NextDouble(); // 0.0 ~ 1.0
        double sqrt2 = 1.414213562373095; // Math.Sqrt(2)

        return Mean + InverseErrorFunc(2 * u - 1) * sqrt2 * Std;
    }
}
