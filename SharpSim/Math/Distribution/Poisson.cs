namespace SharpSim;
public class Poisson : Distribution
{
    public Poisson(double lambda) : base(DistributionType.Poisson)
    {
        if (lambda <= 0)
            throw new ArgumentException("Lambda must be positive.");
        this.lamdda = lambda;
    }

    public override double GetNumber()
    {
        // Knuth algorithm for lambda < 30; normal approximation for large lambda
        if (lamdda < 30)
        {
            double limit = Math.Exp(-lamdda);
            double p = random.NextDouble();
            int k = 0;
            while (p > limit)
            {
                p *= random.NextDouble();
                k++;
            }
            return k;
        }
        else
        {
            // Normal approximation: Poisson(λ) ≈ N(λ, √λ) for large λ
            double u = random.NextDouble();
            double sqrt2 = 1.414213562373095;
            double z = InverseErrorFunc(2 * u - 1) * sqrt2 * Math.Sqrt(lamdda);
            return Math.Max(0, Math.Round(lamdda + z));
        }
    }
}
