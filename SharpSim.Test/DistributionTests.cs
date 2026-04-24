namespace SharpSim.Test;

public class DistributionTests
{
    [Fact]
    public void Poisson_ReturnsNonNegativeInteger()
    {
        var dist = new Poisson(5.0);
        for (int i = 0; i < 1000; i++)
        {
            double value = dist.GetNumber();
            Assert.True(value >= 0);
            Assert.Equal(value, Math.Floor(value)); // integer check
        }
    }

    [Fact]
    public void Poisson_MeanApproximatesLambda()
    {
        double lambda = 10.0;
        var dist = new Poisson(lambda);
        int n = 100_000;
        double sum = 0;
        for (int i = 0; i < n; i++)
            sum += dist.GetNumber();
        double empiricalMean = sum / n;
        Assert.InRange(empiricalMean, lambda * 0.98, lambda * 1.02);
    }
    

    [Fact]
    public void Exponential_MeanApproximatesParameter()
    {
        double mean = 5.0;
        var dist = new Exponential(mean);
        int n = 100_000;
        double sum = 0;
        for (int i = 0; i < n; i++)
            sum += dist.GetNumber();
        double empiricalMean = sum / n;
        Assert.InRange(empiricalMean, mean * 0.98, mean * 1.02);
    }
}
