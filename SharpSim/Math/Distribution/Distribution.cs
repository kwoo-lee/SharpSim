namespace SharpSim;
public abstract class Distribution(DistributionType type)
{
    protected static Random random = new Random(0);
    public DistributionType Type { get; private set;} = type;
    public double Min { get; protected set;}
    public double Max { get; protected set;}
    public double Mode { get; protected set;}
    public double Mean { get; protected set;}
    public double Std { get; protected set;}
    protected double alpha;
    protected double beta;
    protected double lamdda;

    public virtual double GetNumber()
    {
        return 0;
    }

    protected double InverseErrorFunc(double x)
    {
        double a = 0.140012;
        double signal = Signal(x);
        double err = Math.Log(1 - (x * x));
        double value1 = (2 / (Math.PI * a)) + (err / 2);
        double value2 = err / a;

        double result = Math.Sqrt(value1 * value1 - value2) - value1;
        return signal * Math.Sqrt(result);
    }

    protected double Signal(double x)
    {
        if (x > 0) return 1;
        else if (x == 0) return 0;
        else return -1;
    }
}

public class Const : Distribution
{
    public Const(double mean) : base(DistributionType.Constant)
    {
        this.Mean = mean;
    }

    public override double GetNumber()
    {
        return this.Mean;
    }
}
