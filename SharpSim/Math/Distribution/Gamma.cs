namespace SharpSim;
public class Gamma : Distribution
{
    public Gamma(double alpha) : base(DistributionType.Gamma)
    {
        this.alpha = alpha;
    }

    public override double GetNumber()
    {
        double u = random.NextDouble();
        double result = beta * GammaLowerRegularizedInv(alpha, u);
        if (double.IsNaN(result) || double.IsInfinity(result))
            new ArgumentException("Result is not valid");

        return result;
    }


    static readonly double[] _gammaDk =
    {
        2.48574089138753565546e-5,
        1.05142378581721974210,
        -3.45687097222016235469,
        4.51227709466894823700,
       -2.98285225323576655721,
       1.05639711577126713077,
         -1.95428773191645869583e-1,
         1.70970543404441224307e-2,
        -5.71926117404305781283e-4,
        4.63399473359905636708e-6,
        -2.71994908488607703910e-9
    };

    private const int _gammaN = 10;
    private const double _gammaR = 10.900511;
    const double _twoSqrtEOverPi = 1.8603827342052657173362492472666631120594218414085755;
    const double _logTwoSqrtEOverPi = 0.6207822376352452223455184457816472122518527279025978;
    const double _lnPi = 1.1447298858494001741434273513530587116472948129153;

    private double GammaFunction(double z)
    {
        if (z < 0.5)
        {
            double s = _gammaDk[0];
            for (int i = 1; i <= _gammaN; i++)
            {
                s += _gammaDk[i] / (i - z);
            }

            return Math.PI / (Math.Sin(Math.PI * z) * s * _twoSqrtEOverPi * Math.Pow((0.5 - z + _gammaR) / Math.E, 0.5 - z));
        }
        else
        {
            double s = _gammaDk[0];
            for (int i = 1; i <= _gammaN; i++)
            {
                s += _gammaDk[i] / (z + i - 1.0);
            }

            return s * _twoSqrtEOverPi * Math.Pow((z - 0.5 + _gammaR) / Math.E, z - 0.5);
        }
    }
    private double GammaLowerRegularized(double a, double x)
    {
        const double epsilon = 0.000000000000001;
        const double big = 4503599627370496.0;
        const double bigInv = 2.22044604925031308085e-16;

        if (a < 0d)
        {
            throw new Exception("Error a");
        }
        if (x < 0d)
        {
            throw new Exception("Error x");
        }

        if (IsAlmostEqual(a, 0.0))
        {
            if (IsAlmostEqual(x, 0.0))
            {
                return Double.NaN;
            }

            return 1d;
        }

        if (IsAlmostEqual(x, 0.0))
        {
            return 0d;
        }

        double ax = (a * Math.Log(x)) - x - GammaLn(a);
        if (ax < -709.78271289338399)
        {
            return a < x ? 1d : 0d;
        }

        if (x <= 1 || x <= a)
        {
            double r2 = a;
            double c2 = 1;
            double ans2 = 1;

            do
            {
                r2 = r2 + 1;
                c2 = c2 * x / r2;
                ans2 += c2;
            }
            while ((c2 / ans2) > epsilon);

            return Math.Exp(ax) * ans2 / a;
        }

        int c = 0;
        double y = 1 - a;
        double z = x + y + 1;

        double p3 = 1;
        double q3 = x;
        double p2 = x + 1;
        double q2 = z * x;
        double ans = p2 / q2;

        double error;

        do
        {
            c++;
            y += 1;
            z += 2;
            double yc = y * c;

            double p = (p2 * z) - (p3 * yc);
            double q = (q2 * z) - (q3 * yc);

            if (q != 0)
            {
                double nextans = p / q;
                error = Math.Abs((ans - nextans) / nextans);
                ans = nextans;
            }
            else
            {
                error = 1;
            }
            p3 = p2;
            p2 = p;
            q3 = q2;
            q2 = q;
            if (Math.Abs(p) > big)
            {
                p3 *= bigInv;
                p2 *= bigInv;
                q3 *= bigInv;
                q2 *= bigInv;
            }
        }
        while (error > epsilon);

        return 1d - (Math.Exp(ax) * ans);
    }
    private double GammaLowerRegularizedInv(double a, double y0)
    {
        const double epsilon = 0.000000000000001;
        const double big = 4503599627370496.0;
        const double threshold = 5 * epsilon;

        if (double.IsNaN(a) || double.IsNaN(y0))
        {
            return double.NaN;
        }

        if (a < 0 || IsAlmostEqual(a, 0.0))
        {
            throw new ArgumentOutOfRangeException("a");
        }
        if (y0 < 0 || y0 > 1)
        {
            throw new ArgumentOutOfRangeException("y0");
        }

        if (IsAlmostEqual(y0, 0.0))
        {
            return 0d;
        }

        if (IsAlmostEqual(y0, 1.0))
        {
            return Double.PositiveInfinity;
        }

        y0 = 1 - y0;

        double xUpper = big;
        double xLower = 0;
        double yUpper = 1;
        double yLower = 0;

        // Initial Guess
        double d = 1 / (9 * a);
        double y = 1 - d - (0.98 * Math.Sqrt(2) * InverseErrorFunc((2.0 * y0) - 1.0) * Math.Sqrt(d));
        double x = a * y * y * y;
        double lgm = GammaLn(a);

        for (int i = 0; i < 10; i++)
        {
            if (x < xLower || x > xUpper)
            {
                d = 0.0625;
                break;
            }

            y = 1 - GammaLowerRegularized(a, x);
            if (y < yLower || y > yUpper)
            {
                d = 0.0625;
                break;
            }

            if (y < y0)
            {
                xUpper = x;
                yLower = y;
            }
            else
            {
                xLower = x;
                yUpper = y;
            }

            d = ((a - 1) * Math.Log(x)) - x - lgm;
            if (d < -709.78271289338399)
            {
                d = 0.0625;
                break;
            }

            d = -Math.Exp(d);
            d = (y - y0) / d;
            if (Math.Abs(d / x) < epsilon)
            {
                return x;
            }

            if ((d > (x / 4)) && (y0 < 0.05))
            {
                // Naive heuristics for cases near the singularity
                d = x / 10;
            }

            x -= d;
        }

        if (xUpper == big)
        {
            if (x <= 0)
            {
                x = 1;
            }

            while (xUpper == big)
            {
                x = (1 + d) * x;
                y = 1 - GammaLowerRegularized(a, x);
                if (y < y0)
                {
                    xUpper = x;
                    yLower = y;
                    break;
                }

                d = d + d;
            }
        }

        int dir = 0;
        d = 0.5;
        for (int i = 0; i < 400; i++)
        {
            x = xLower + (d * (xUpper - xLower));
            y = 1 - GammaLowerRegularized(a, x);
            lgm = (xUpper - xLower) / (xLower + xUpper);
            if (Math.Abs(lgm) < threshold)
            {
                return x;
            }

            lgm = (y - y0) / y0;
            if (Math.Abs(lgm) < threshold)
            {
                return x;
            }

            if (x <= 0d)
            {
                return 0d;
            }

            if (y >= y0)
            {
                xLower = x;
                yUpper = y;
                if (dir < 0)
                {
                    dir = 0;
                    d = 0.5;
                }
                else
                {
                    if (dir > 1)
                    {
                        d = (0.5 * d) + 0.5;
                    }
                    else
                    {
                        d = (y0 - yLower) / (yUpper - yLower);
                    }
                }

                dir = dir + 1;
            }
            else
            {
                xUpper = x;
                yLower = y;
                if (dir > 0)
                {
                    dir = 0;
                    d = 0.5;
                }
                else
                {
                    if (dir < -1)
                    {
                        d = 0.5 * d;
                    }
                    else
                    {
                        d = (y0 - yLower) / (yUpper - yLower);
                    }
                }

                dir = dir - 1;
            }
        }

        return x;
    }
    private double GammaLn(double z)
    {
        if (z < 0.5)
        {
            double s = _gammaDk[0];
            for (int i = 1; i <= _gammaN; i++)
            {
                s += _gammaDk[i] / (i - z);
            }

            return _lnPi
                   - Math.Log(Math.Sin(Math.PI * z))
                   - Math.Log(s)
                   - _logTwoSqrtEOverPi
                   - ((0.5 - z) * Math.Log((0.5 - z + _gammaR) / Math.E));
        }
        else
        {
            double s = _gammaDk[0];
            for (int i = 1; i <= _gammaN; i++)
            {
                s += _gammaDk[i] / (z + i - 1.0);
            }

            return Math.Log(s)
                   + _logTwoSqrtEOverPi
                   + ((z - 0.5) * Math.Log((z - 0.5 + _gammaR) / Math.E));
        }
    }
    private Boolean IsAlmostEqual(double a, double b)
    {
        return Math.Abs(a - b) < 0.00000000000000000000000000000000000000000001;
    }
    

}
