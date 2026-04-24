#region License
/*
MIT License
Copyright © 2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

namespace SharpSim;

[Serializable]
public struct Vector2 : IEquatable<Vector2>
{
    #region Private Fields

    private static readonly Vector2 zeroVector = new(0, 0);
    private static readonly Vector2 unitVector = new(1, 1);
    private static readonly Vector2 unitXVector = new(1, 0);
    private static readonly Vector2 unitYVector = new(0, 1);

    #endregion Private Fields


    #region Public Fields

    public double X;
    public double Y;

    #endregion Public Fields


    #region Properties
    public double Length => Math.Sqrt((X * X) + (Y * Y));

    public double LengthSquared => (X * X) + (Y * Y);

    public static Vector2 Zero => zeroVector;

    public static Vector2 One => unitVector;

    public static Vector2 UnitX => unitXVector;

    public static Vector2 UnitY => unitYVector;

    #endregion Properties


    #region Constructors
    public Vector2(double x, double y)
    {
        this.X = x;
        this.Y = y;
    }

    public Vector2(double value)
    {
        this.X = value;
        this.Y = value;
    }

    public Vector2(Vector3 vector3, Vector3.Plane plane)
    {
        switch (plane)
        {
            case Vector3.Plane.XY:
                X = vector3.X;
                Y = vector3.Y;
                break;
            case Vector3.Plane.YZ:
                X = vector3.Y;
                Y = vector3.Z;
                break;
            case Vector3.Plane.XZ:
                X = vector3.X;
                Y = vector3.Z;
                break;
            default:
                X = -1;
                Y = -1;
                break;
        }
    }
    #endregion Constructors


    #region Public Methods

    public static Vector2 Add(Vector2 value1, Vector2 value2)
    {
        value1.X += value2.X;
        value1.Y += value2.Y;
        return value1;
    }

    public static void Add(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
    {
        result.X = value1.X + value2.X;
        result.Y = value1.Y + value2.Y;
    }

    public static double AngleDegree(Vector2 value1, Vector2 value2)
    {
        // dot(= value1 * value2) == |value1| x |value2| x cos(theta)
        return AngleRadian(value1, value2) * 180 / Math.PI;
    }

    public static double AngleRadian(Vector2 value1, Vector2 value2)
    {
        if (value1.Length == 0) return -1;
        if (value2.Length == 0) return -1;
        // dot(= value1 * value2) == |value1| x |value2| x cos(theta)
        double theta = Math.Acos(Math.Round(Dot(value1, value2) / (value1.Length * value2.Length),5));
        if (Double.IsNaN(theta))
            return 0;
        else
            return theta;
    }

    public static double AbsoluteAngleDegree(Vector2 value1, Vector2 value2)
    {
        // dot(= value1 * value2) == |value1| x |value2| x cos(theta)
        return AbsoluteAngleRadian(value1, value2) * 180 / Math.PI;
    }

    public double AbsoluteAngleDegree()
    {
        return AbsoluteAngleRadian() * 180 / Math.PI;
    }

    public double AbsoluteAngleRadian()
    {
        return Vector2.AbsoluteAngleRadian(new Vector2(1, 0), this);
    }

    public static double AbsoluteAngleRadian(Vector2 value1, Vector2 value2)
    {
        // dot(= value1 * value2) == |value1| x |value2| x cos(theta)
        var cross = CrossProduct(value1, value2);
        //double theta = Math.Asin(CrossProduct(value1, value2) / (value1.Length * value2.Length));
        double theta = Math.Acos(Dot(value1, value2) / (value1.Length * value2.Length));
        if (cross < 0)
            theta = 2 * Math.PI - theta;
        return theta;
    }

    public static double AbsoluteAngleRadian(Vector2 value)
    {
        // dot(= value1 * value2) == |value1| x |value2| x cos(theta)
        var cross = CrossProduct(new Vector2(1, 0), value);
        //double theta = Math.Asin(CrossProduct(value1, value2) / (value1.Length * value2.Length));
        double theta = Math.Acos(Dot(new Vector2(1, 0), value) / (new Vector2(1, 0).Length * value.Length));
        if (cross < 0)
            theta = 2 * Math.PI - theta;
        return theta;
    }

    public static Vector2 Barycentric(Vector2 value1, Vector2 value2, Vector2 value3, double amount1, double amount2)
    {
        return new Vector2(
            MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
            MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));
    }

    public static void Barycentric(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, double amount1, double amount2, out Vector2 result)
    {
        result = new Vector2(
            MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
            MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2));
    }

    public static double CrossProduct(Vector2 value1, Vector2 value2)
    {
        return value1.X * value2.Y - value2.X * value1.Y;
    }

    public static Vector2 CatmullRom(Vector2 value1, Vector2 value2, Vector2 value3, Vector2 value4, double amount)
    {
        return new Vector2(
            MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
            MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
    }

    public static void CatmullRom(ref Vector2 value1, ref Vector2 value2, ref Vector2 value3, ref Vector2 value4, double amount, out Vector2 result)
    {
        result = new Vector2(
            MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
            MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount));
    }

    public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
    {
        return new Vector2(
            MathHelper.Clamp(value1.X, min.X, max.X),
            MathHelper.Clamp(value1.Y, min.Y, max.Y));
    }

    public static void Clamp(ref Vector2 value1, ref Vector2 min, ref Vector2 max, out Vector2 result)
    {
        result = new Vector2(
            MathHelper.Clamp(value1.X, min.X, max.X),
            MathHelper.Clamp(value1.Y, min.Y, max.Y));
    }

    public static double Distance(Vector2 value1, Vector2 value2)
    {
        double v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
        return (double)Math.Sqrt((v1 * v1) + (v2 * v2));
    }

    public static void Distance(ref Vector2 value1, ref Vector2 value2, out double result)
    {
        double v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
        result = (double)Math.Sqrt((v1 * v1) + (v2 * v2));
    }

    public static double DistanceSquared(Vector2 value1, Vector2 value2)
    {
        double v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
        return (v1 * v1) + (v2 * v2);
    }

    public static void DistanceSquared(ref Vector2 value1, ref Vector2 value2, out double result)
    {
        double v1 = value1.X - value2.X, v2 = value1.Y - value2.Y;
        result = (v1 * v1) + (v2 * v2);
    }

    public static DirectionType Direction(Vector2 from, Vector2 to, Vector2 otherVec2)
    {
        var crossProduct = CrossProduct((otherVec2 - from), (to - from));

        if (crossProduct > 0 && crossProduct < 0.0001)
            return DirectionType.Colinear;
        else if (crossProduct < 0 && crossProduct > -0.0001)
            return DirectionType.Colinear;
        else if(crossProduct > 0)
            return DirectionType.ClockWise;
        else if (crossProduct < 0)
            return DirectionType.CounterClockWise;
        else
            return DirectionType.Colinear;

    }

    public static Vector2 Divide(Vector2 value1, Vector2 value2)
    {
        value1.X /= value2.X;
        value1.Y /= value2.Y;
        return value1;
    }

    public static void Divide(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
    {
        result.X = value1.X / value2.X;
        result.Y = value1.Y / value2.Y;
    }

    public static Vector2 Divide(Vector2 value1, double divider)
    {
        double factor = 1 / divider;
        value1.X *= factor;
        value1.Y *= factor;
        return value1;
    }

    public static void Divide(ref Vector2 value1, double divider, out Vector2 result)
    {
        double factor = 1 / divider;
        result.X = value1.X * factor;
        result.Y = value1.Y * factor;
    }

    public static double Dot(Vector2 value1, Vector2 value2)
    {
        return (value1.X * value2.X) + (value1.Y * value2.Y);
    }

    public static void Dot(ref Vector2 value1, ref Vector2 value2, out double result)
    {
        result = (value1.X * value2.X) + (value1.Y * value2.Y);
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector2)
        {
            return Equals((Vector2)this);
        }

        return false;
    }

    public bool Equals(Vector2 other)
    {
        return (X == other.X) && (Y == other.Y);
    }

    public static Vector2 Reflect(Vector2 vector, Vector2 normal)
    {
        Vector2 result;
        double val = 2.0f * ((vector.X * normal.X) + (vector.Y * normal.Y));
        result.X = vector.X - (normal.X * val);
        result.Y = vector.Y - (normal.Y * val);
        return result;
    }

    public static void Reflect(ref Vector2 vector, ref Vector2 normal, out Vector2 result)
    {
        double val = 2.0f * ((vector.X * normal.X) + (vector.Y * normal.Y));
        result.X = vector.X - (normal.X * val);
        result.Y = vector.Y - (normal.Y * val);
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() + Y.GetHashCode();
    }

    public static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, double amount)
    {
        Vector2 result = new Vector2();
        Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
        return result;
    }

    public static void Hermite(ref Vector2 value1, ref Vector2 tangent1, ref Vector2 value2, ref Vector2 tangent2, double amount, out Vector2 result)
    {
        result.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
        result.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
    }

    public bool IsNAN()
    {
        return IsNAN(this);
    }

    public static bool IsNAN(Vector2 value)
    {
        return (double.IsNaN(value.X) || double.IsNaN(value.Y));
    }

    public static Vector2 Lerp(Vector2 value1, Vector2 value2, double amount)
    {
        return new Vector2(
            MathHelper.Lerp(value1.X, value2.X, amount),
            MathHelper.Lerp(value1.Y, value2.Y, amount));
    }

    public static void Lerp(ref Vector2 value1, ref Vector2 value2, double amount, out Vector2 result)
    {
        result = new Vector2(
            MathHelper.Lerp(value1.X, value2.X, amount),
            MathHelper.Lerp(value1.Y, value2.Y, amount));
    }

    public static Vector2 Max(Vector2 value1, Vector2 value2)
    {
        return new Vector2(value1.X > value2.X ? value1.X : value2.X,
                           value1.Y > value2.Y ? value1.Y : value2.Y);
    }

    public static void Max(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
    {
        result.X = value1.X > value2.X ? value1.X : value2.X;
        result.Y = value1.Y > value2.Y ? value1.Y : value2.Y;
    }

    public static Vector2 Min(Vector2 value1, Vector2 value2)
    {
        return new Vector2(value1.X < value2.X ? value1.X : value2.X,
                           value1.Y < value2.Y ? value1.Y : value2.Y);
    }

    public static void Min(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
    {
        result.X = value1.X < value2.X ? value1.X : value2.X;
        result.Y = value1.Y < value2.Y ? value1.Y : value2.Y;
    }

    public static Vector2 Multiply(Vector2 value1, Vector2 value2)
    {
        value1.X *= value2.X;
        value1.Y *= value2.Y;
        return value1;
    }

    public static Vector2 Multiply(Vector2 value1, double scaleFactor)
    {
        value1.X *= scaleFactor;
        value1.Y *= scaleFactor;
        return value1;
    }

    public static void Multiply(ref Vector2 value1, double scaleFactor, out Vector2 result)
    {
        result.X = value1.X * scaleFactor;
        result.Y = value1.Y * scaleFactor;
    }

    public static void Multiply(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
    {
        result.X = value1.X * value2.X;
        result.Y = value1.Y * value2.Y;
    }

    public static Vector2 Negate(Vector2 value)
    {
        value.X = -value.X;
        value.Y = -value.Y;
        return value;
    }

    public static void Negate(ref Vector2 value, out Vector2 result)
    {
        result.X = -value.X;
        result.Y = -value.Y;
    }

    public void Normalize()
    {
        double val = 1.0f / (double)Math.Sqrt((X * X) + (Y * Y));
        X *= val;
        Y *= val;
    }

    public static Vector2 Normalize(Vector2 value)
    {
        double val = 1.0f / (double)Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
        value.X *= val;
        value.Y *= val;
        return value;
    }

    public static void Normalize(ref Vector2 value, out Vector2 result)
    {
        double val = 1.0f / (double)Math.Sqrt((value.X * value.X) + (value.Y * value.Y));
        result.X = value.X * val;
        result.Y = value.Y * val;
    }

    public static Vector2 SmoothStep(Vector2 value1, Vector2 value2, double amount)
    {
        return new Vector2(
            MathHelper.SmoothStep(value1.X, value2.X, amount),
            MathHelper.SmoothStep(value1.Y, value2.Y, amount));
    }

    public static void SmoothStep(ref Vector2 value1, ref Vector2 value2, double amount, out Vector2 result)
    {
        result = new Vector2(
            MathHelper.SmoothStep(value1.X, value2.X, amount),
            MathHelper.SmoothStep(value1.Y, value2.Y, amount));
    }

    public static Vector2 Subtract(Vector2 value1, Vector2 value2)
    {
        value1.X -= value2.X;
        value1.Y -= value2.Y;
        return value1;
    }

    public static void Subtract(ref Vector2 value1, ref Vector2 value2, out Vector2 result)
    {
        result.X = value1.X - value2.X;
        result.Y = value1.Y - value2.Y;
    }

    public Vector2 RotateByDegree(double degree)
    {
        return RotateByRadian(degree * Math.PI / 180);
    }

    public Vector2 RotateByRadian(double radian)
    {
        return Vector2.RotateByRadian(this, radian);
    }

    public static Vector2 RotateByDegree(Vector2 value, double degree)
    {
        return Vector2.RotateByRadian(value, degree * Math.PI / 180);
    }

    public static Vector2 RotateByRadian(Vector2 value, double radian)
    {
        //cos *x - sin * y, sin* x + cos * y
        var cos = Math.Cos(radian);
        var sin = Math.Sin(radian);

        return new Vector2(cos * value.X - sin * value.Y, sin * value.X + cos * value.Y);
    }

    public static Vector2 Transform(Vector2 position, Matrix matrix)
    {
        Transform(ref position, ref matrix, out position);
        return position;
    }

    public static void Transform(ref Vector2 position, ref Matrix matrix, out Vector2 result)
    {
        result = new Vector2((position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41,
                             (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42);
    }

    public static Vector2 Transform(Vector2 position, Quaternion quat)
    {
        Transform(ref position, ref quat, out position);
        return position;
    }

    public static void Transform(ref Vector2 position, ref Quaternion quat, out Vector2 result)
    {
        Quaternion v = new Quaternion(position.X, position.Y, 0, 0), i, t;
        Quaternion.Inverse(ref quat, out i);
        Quaternion.Multiply(ref quat, ref v, out t);
        Quaternion.Multiply(ref t, ref i, out v);

        result = new Vector2(v.X, v.Y);
    }

    public static void Transform(
        Vector2[] sourceArray,
        ref Matrix matrix,
        Vector2[] destinationArray)
    {
        Transform(sourceArray, 0, ref matrix, destinationArray, 0, sourceArray.Length);
    }


    public static void Transform(
        Vector2[] sourceArray,
        int sourceIndex,
        ref Matrix matrix,
        Vector2[] destinationArray,
        int destinationIndex,
        int length)
    {
        for (int x = 0; x < length; x++)
        {
            var position = sourceArray[sourceIndex + x];
            var destination = destinationArray[destinationIndex + x];
            destination.X = (position.X * matrix.M11) + (position.Y * matrix.M21) + matrix.M41;
            destination.Y = (position.X * matrix.M12) + (position.Y * matrix.M22) + matrix.M42;
            destinationArray[destinationIndex + x] = destination;
        }
    }

    public static Vector2 TransformNormal(Vector2 normal, Matrix matrix)
    {
        Vector2.TransformNormal(ref normal, ref matrix, out normal);
        return normal;
    }

    public static void TransformNormal(ref Vector2 normal, ref Matrix matrix, out Vector2 result)
    {
        result = new Vector2((normal.X * matrix.M11) + (normal.Y * matrix.M21),
                             (normal.X * matrix.M12) + (normal.Y * matrix.M22));
    }

    public static bool GetIntersectPoint(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2, out Vector2 intersectPoint)
    {
        double under = (end2.Y - start2.Y) * (end1.X - start1.X) - (end2.X - start2.X) * (end1.Y - start1.Y);
        if (under == 0)
            goto FAIL;

        double tempT = ((end2.X - start2.X) * (start1.Y - start2.Y) - (end2.Y - start2.Y) * (start1.X - start2.X));
        double tempS = ((end1.X - start1.X) * (start1.Y - start2.Y) - (end1.Y - start1.Y) * (start1.X - start2.X));

        if (tempT == 0 && tempS == 0) goto FAIL;

        double t = tempT / under, s = tempS / under;
        if (t < 0.0 || t > 1.0 || s < 0.0 || s > 1.0) goto FAIL;

        double x = start1.X + t * (double)(end1.X - start1.X);
        double y = start1.Y + t * (double)(end1.Y - start1.Y);

        intersectPoint = new Vector2(x, y);
        return true;

    FAIL:
        intersectPoint = new Vector2(-1, -1);
        return false;
    }

    public override string ToString() => $"{{X:{X} Y:{Y}}}";
    #endregion Public Methods


    #region Operators
    public static Vector2 operator -(Vector2 value)
    {
        value.X = -value.X;
        value.Y = -value.Y;
        return value;
    }


    public static bool operator ==(Vector2 value1, Vector2 value2)
    {
        return value1.X == value2.X && value1.Y == value2.Y;
    }


    public static bool operator !=(Vector2 value1, Vector2 value2)
    {
        return value1.X != value2.X || value1.Y != value2.Y;
    }


    public static Vector2 operator +(Vector2 value1, Vector2 value2)
    {
        var newVector = new Vector2(value1.X, value1.Y);
        newVector.X += value2.X;
        newVector.Y += value2.Y;
        return newVector;
    }


    public static Vector2 operator -(Vector2 value1, Vector2 value2)
    {
        var newVector = new Vector2(value1.X, value1.Y);
        newVector.X -= value2.X;
        newVector.Y -= value2.Y;
        return newVector;
    }

    public static Vector2 operator *(Vector2 value1, Vector2 value2)
    {
        value1.X *= value2.X;
        value1.Y *= value2.Y;
        return value1;
    }


    public static Vector2 operator *(Vector2 value, double scaleFactor)
    {
        value.X *= scaleFactor;
        value.Y *= scaleFactor;
        return value;
    }


    public static Vector2 operator *(double scaleFactor, Vector2 value)
    {
        value.X *= scaleFactor;
        value.Y *= scaleFactor;
        return value;
    }


    public static Vector2 operator /(Vector2 value1, Vector2 value2)
    {
        value1.X /= value2.X;
        value1.Y /= value2.Y;
        return value1;
    }


    public static Vector2 operator /(Vector2 value1, double divider)
    {
        double factor = 1 / divider;
        value1.X *= factor;
        value1.Y *= factor;
        return value1;
    }

    #endregion Operators
}
