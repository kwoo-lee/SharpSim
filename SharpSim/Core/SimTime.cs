namespace SharpSim;

public struct SimTime : IComparable, IEquatable<SimTime>
{
    private long _value; // Milliseconds
    public readonly double TotalDays    => ToDay();
    public readonly double TotalHours   => ToHours();
    public readonly double TotalMinutes => ToMinutes();
    public readonly double TotalSeconds => ToSecond();

    public SimTime(long second)   { _value = second * 1000; }
    public SimTime(float second)  { _value = (long)second * 1000; }
    public SimTime(double second) { _value = (long)(second * 1000); }

    public SimTime(DateTime dt)
    {
        _value = (long)dt.TimeOfDay.TotalMilliseconds;
    }

    public SimTime(TimeSpan ts)
    {
        _value = (long)ts.TotalMilliseconds;
    }

    #region [Arithmetic Operator]
    public static SimTime operator -(SimTime time)                     => (SimTime)(-time._value);
    public static SimTime operator -(SimTime left, SimTime right)      { SimTime t; t._value = left._value - right._value; return t; }
    public static SimTime operator +(SimTime left, SimTime right)      { SimTime t; t._value = left._value + right._value; return t; }
    #endregion [Arithmetic Operator]

    #region [Logical Operator]
    public static bool operator ==(SimTime left, SimTime right)  => left._value == right._value;
    public static bool operator ==(SimTime left, double right)   => left._value == right;
    public static bool operator ==(double left, SimTime right)   => left == right._value;
    public static bool operator ==(SimTime left, float right)    => left._value == right;
    public static bool operator ==(float left, SimTime right)    => right._value == left;

    public static bool operator !=(SimTime left, SimTime right)  => left._value != right._value;
    public static bool operator !=(SimTime left, double right)   => left._value != right;
    public static bool operator !=(double left, SimTime right)   => left != right._value;
    public static bool operator !=(SimTime left, float right)    => left._value != right;
    public static bool operator !=(float left, SimTime right)    => right._value != left;

    public static bool operator < (SimTime left, SimTime right)  => left._value <  right._value;
    public static bool operator <=(SimTime left, SimTime right)  => left._value <= right._value;
    public static bool operator > (SimTime left, SimTime right)  => left._value >  right._value;
    public static bool operator >=(SimTime left, SimTime right)  => left._value >= right._value;
    #endregion [Logical Operator]

    #region [Operator %]
    public static SimTime operator %(SimTime left, SimTime right) => left._value % right._value;
    public static SimTime operator %(SimTime left, double right)  => left._value % right;
    public static SimTime operator %(double left, SimTime right)  => left % right._value;
    public static SimTime operator %(SimTime left, float right)   => left._value % right;
    public static SimTime operator %(float left, SimTime right)   => left % right._value;
    #endregion [Operator %]

    #region [Operator *]
    public static SimTime operator *(SimTime left, SimTime right) => left._value * right._value;
    public static SimTime operator *(SimTime left, double right)  => left._value * right;
    public static SimTime operator *(double left, SimTime right)  => left * right._value;
    public static SimTime operator *(SimTime left, float right)   => left._value * right;
    public static SimTime operator *(float left, SimTime right)   => left * right._value;
    #endregion [Operator *]

    #region [Operator /]
    public static SimTime operator /(SimTime left, SimTime right) => left._value / right._value;
    public static SimTime operator /(SimTime left, double right)  => left._value / right;
    public static SimTime operator /(double left, SimTime right)  => left / right._value;
    public static SimTime operator /(SimTime left, float right)   => left._value / right;
    public static SimTime operator /(float left, SimTime right)   => left / right._value;
    #endregion [Operator /]

    #region [Casting Operator]
    public static explicit operator double(SimTime time)   => time.ToSecond();
    public static explicit operator float(SimTime time)    => (float)time.ToSecond();
    public static explicit operator TimeSpan(SimTime time) => new TimeSpan(0, 0, 0, 0, (int)time._value);
    public static explicit operator DateTime(SimTime time) => new DateTime() + (TimeSpan)time;

    public static implicit operator SimTime(double d)       => new SimTime(d);
    public static implicit operator SimTime(float f)        => new SimTime(f);
    public static implicit operator SimTime(DateTime dt)    => new SimTime(dt.Year);
    public static implicit operator SimTime(TimeSpan ts)    => new SimTime(ts);
    #endregion [Casting Operator]

    #region [Interface Implementation]
    public int CompareTo(object? other)
    {
        if (other is SimTime time)
        {
            return _value.CompareTo(time._value);
        }
        return 1;
    }

    public bool Equals(SimTime other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SimTime other && Equals(other);
    }

    public override int GetHashCode()
    {
        byte[] data = BitConverter.GetBytes(_value);
        return BitConverter.ToInt32(data, 0) ^ BitConverter.ToInt32(data, 4);
    }

    public override readonly string ToString() => ToSecond().ToString();

    public string ToString(string format)
    {
        return ((DateTime)new SimTime(_value)).ToString(format);
    }
    #endregion [Interface Implementation]

    #region [Conversion Methods]
    public readonly int    ToDay()    => (int)(_value / (3600L * 24 * 1000));
    public readonly int    ToHours()  => (int)(_value / (3600L * 1000));
    public readonly int    ToMinutes()=> (int)(_value / (60L * 1000));
    public readonly double ToSecond() => _value / 1000.0;
    #endregion [Conversion Methods]

    #region [Factory Methods]
    public static SimTime FromDays(int day)        { SimTime t; t._value = (long)day  * 3600 * 24 * 1000; return t; }
    public static SimTime FromHours(int hour)      { SimTime t; t._value = (long)hour * 3600 * 1000;      return t; }
    public static SimTime FromMinutes(int min)     { SimTime t; t._value = (long)min  * 60   * 1000;      return t; }
    public static SimTime FromSeconds(int sec)     { SimTime t; t._value = (long)sec  * 1000;             return t; }
    public static SimTime FromMilliseconds(int ms) { SimTime t; t._value = ms;                            return t; }
    #endregion [Factory Methods]

    #region [Math Helpers]
    public static SimTime Max(SimTime left, SimTime right) => left._value >= right._value ? left : right;
    public static SimTime Min(SimTime left, SimTime right) => left._value <= right._value ? left : right;
    public static SimTime Abs(SimTime time)                => time._value >= 0 ? time : -time;
    #endregion [Math Helpers]

    #region [Mutating Methods]
    public void AddSeconds(double seconds)  { _value += (long)(seconds * 1000); }
    public void AddMinutes(double minutes)  { _value += (long)(minutes * 60 * 1000); }
    #endregion [Mutating Methods]
}
