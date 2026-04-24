namespace SharpSim;
public class LogNormal : Distribution
{
    public LogNormal(double mean, double std) : base(DistributionType.Lognormal)
    {
        this.Mean = mean;
        this.Std = std;
    }

    public override double GetNumber()
    {
        // Parameter 
        var v = Math.Log((Std * Std) / (Mean * Mean) + 1);
        var m = Math.Log(Mean) - v / 2;

        double u = random.NextDouble();
        double sqrt2 = 1.414213562373095; // Math.Sqrt(2)
        double tmp = (InverseErrorFunc(2 * u - 1) * sqrt2 * Math.Sqrt(v)) + m;
        double result = Math.Exp(tmp);
        if (double.IsNaN(result) || double.IsInfinity(result))
            new ArgumentException("Result is not valid");

        return result;
    }
}
