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

using System.Diagnostics;

namespace SharpSim;
[Serializable]
public struct Vector3 : IEquatable<Vector3>
{
    public enum Coordinate
    {
        X, Y, Z
    }

    public enum Plane
    {
        XY, YZ, XZ
    }
    #region Private Fields

    private static readonly Vector3 zero = new(0, 0, 0);
    private static readonly Vector3 one = new(1, 1, 1);
    private static readonly Vector3 unitX = new(1, 0, 0);
    private static readonly Vector3 unitY = new(0, 1, 0);
    private static readonly Vector3 unitZ = new(0, 0, 1);
    private static readonly Vector3 up = new(0, 1, 0);
    private static readonly Vector3 down = new(0, -1, 0);
    private static readonly Vector3 right = new(1, 0, 0);
    private static readonly Vector3 left = new(-1, 0, 0);
    private static readonly Vector3 forward = new(0, 0, -1);
    private static readonly Vector3 backward = new(0, 0, 1);

    #endregion Private Fields

    #region Public Fields

    public double X;
    public double Y;
    public double Z;

    #endregion Public Fields

    #region Properties

    public static Vector3 Zero => zero;

    public static Vector3 One => one;

    public static Vector3 UnitX => unitX;

    public static Vector3 UnitY => unitY;

    public static Vector3 UnitZ => unitZ;

    public static Vector3 Up => up;

    public static Vector3 Down => down;

    public static Vector3 Right => right;

    public static Vector3 Left => left;

    public static Vector3 Forward => forward;

    public static Vector3 Backward => backward;

    #endregion Properties

    #region Constructors

    public Vector3(double x, double y, double z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }


    public Vector3(double value)
    {
        this.X = value;
        this.Y = value;
        this.Z = value;
    }


    public Vector3(Vector2 value, double z)
    {
        this.X = value.X;
        this.Y = value.Y;
        this.Z = z;
    }

    public Vector3(Vector3 value)
    {
        this.X = value.X;
        this.Y = value.Y;
        this.Z = value.Z;
    }
    #endregion Constructors

    #region Public Methods
    public static Vector3 Add(Vector3 value1, Vector3 value2)
    {
        value1.X += value2.X;
        value1.Y += value2.Y;
        value1.Z += value2.Z;
        return value1;
    }

    public static void Add(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
    {
        result.X = value1.X + value2.X;
        result.Y = value1.Y + value2.Y;
        result.Z = value1.Z + value2.Z;
    }

    public static double AngleDegree(Vector3 value1, Vector3 value2, Coordinate coordinate)
    {
        return Vector3.AngleRadian(value1, value2, coordinate) * 180 / Math.PI;
    }

    public static double AngleRadian(Vector3 value1, Vector3 value2, Coordinate coordinate)
    {
        Vector2 vector1, vector2;
        switch (coordinate)
        {
            case Coordinate.X:
                vector1 = new Vector2(value1.X, value1.Y);
                vector2 = new Vector2(value2.X, value2.Y);
                break;
            case Coordinate.Y:
                vector1 = new Vector2(value1.X, value1.Y);
                vector2 = new Vector2(value2.X, value2.Y);
                break;
            case Coordinate.Z:
                vector1 = new Vector2(value1.X, value1.Y);
                vector2 = new Vector2(value2.X, value2.Y);
                break;
            default:
                return 0;
        }

        return Vector2.AngleRadian(vector1, vector2);
    }

    public static double AbsoluteAngleDegree(Vector3 value1, Vector3 value2, Coordinate coordinate)
    {
        return Vector3.AbsoluteAngleRadian(value1, value2, coordinate) * 180 / Math.PI;
    }

    public static double AbsoluteAngleRadian(Vector3 value1, Vector3 value2, Coordinate coordinate)
    {
        Vector2 vector1, vector2;
        switch (coordinate)
        {
            case Coordinate.X:
                vector1 = new Vector2(value1.X, value1.Y);
                vector2 = new Vector2(value2.X, value2.Y);
                break;
            case Coordinate.Y:
                vector1 = new Vector2(value1.X, value1.Y);
                vector2 = new Vector2(value2.X, value2.Y);
                break;
            case Coordinate.Z:
                vector1 = new Vector2(value1.X, value1.Y);
                vector2 = new Vector2(value2.X, value2.Y);
                break;
            default:
                return 0;
        }

        return Vector2.AbsoluteAngleRadian(vector1, vector2);
    }

    public double AbsoluteAngleRadian(Coordinate coordinate)
    {
        Vector3 standard;
        switch (coordinate)
        {
            case Coordinate.X:
                standard = new Vector3(0, 1, 0);
                break;
            case Coordinate.Y:
                standard = new Vector3(0, 0, 1);
                break;
            case Coordinate.Z:
                standard = new Vector3(1, 0, 0);
                break;
            default:
                return 0;
        }
        return AbsoluteAngleRadian(standard, this, coordinate);
    }

    public double AbsoluteAngleDegree(Coordinate coordinate)
    {
        return AbsoluteAngleRadian(coordinate) * 180 / Math.PI;
    }

    public static Vector3 Barycentric(Vector3 value1, Vector3 value2, Vector3 value3, double amount1, double amount2)
    {
        return new Vector3(
            MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
            MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2),
            MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2));
    }

    public static void Barycentric(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, double amount1, double amount2, out Vector3 result)
    {
        result = new Vector3(
            MathHelper.Barycentric(value1.X, value2.X, value3.X, amount1, amount2),
            MathHelper.Barycentric(value1.Y, value2.Y, value3.Y, amount1, amount2),
            MathHelper.Barycentric(value1.Z, value2.Z, value3.Z, amount1, amount2));
    }

    public static Vector3 CatmullRom(Vector3 value1, Vector3 value2, Vector3 value3, Vector3 value4, double amount)
    {
        return new Vector3(
            MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
            MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount),
            MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount));
    }

    public static void CatmullRom(ref Vector3 value1, ref Vector3 value2, ref Vector3 value3, ref Vector3 value4, double amount, out Vector3 result)
    {
        result = new Vector3(
            MathHelper.CatmullRom(value1.X, value2.X, value3.X, value4.X, amount),
            MathHelper.CatmullRom(value1.Y, value2.Y, value3.Y, value4.Y, amount),
            MathHelper.CatmullRom(value1.Z, value2.Z, value3.Z, value4.Z, amount));
    }

    public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max)
    {
        return new Vector3(
            MathHelper.Clamp(value1.X, min.X, max.X),
            MathHelper.Clamp(value1.Y, min.Y, max.Y),
            MathHelper.Clamp(value1.Z, min.Z, max.Z));
    }

    public static void Clamp(ref Vector3 value1, ref Vector3 min, ref Vector3 max, out Vector3 result)
    {
        result = new Vector3(
            MathHelper.Clamp(value1.X, min.X, max.X),
            MathHelper.Clamp(value1.Y, min.Y, max.Y),
            MathHelper.Clamp(value1.Z, min.Z, max.Z));
    }

    public static Vector3 Center(Vector3 vector1, Vector3 vector2)
    {
        Center(ref vector1, ref vector2, out vector1);
        return vector1;
    }

    public static void Center(ref Vector3 vector1, ref Vector3 vector2, out Vector3 result)
    {
        result = new Vector3((vector1.X + vector2.X) / 2,
            (vector1.Y + vector2.Y) / 2,
            (vector1.Z + vector2.Z) / 2);
    }

    public static Vector3 Cross(Vector3 vector1, Vector3 vector2)
    {
        Cross(ref vector1, ref vector2, out vector1);
        return vector1;
    }

    public static double Cross(Vector3 value1, Vector3 value2, Coordinate standard)
    {
        switch (standard)
        {
            case Coordinate.X:
                Vector2 val1 = new Vector2(value1.Y, value1.Z);
                Vector2 val2 = new Vector2(value2.Y, value2.Z);
                return Vector2.CrossProduct(val1, val2);
            case Coordinate.Y:
                val1 = new Vector2(value1.X, value1.Z);
                val2 = new Vector2(value2.X, value2.Z);
                return Vector2.CrossProduct(val1, val2);
            case Coordinate.Z:
                val1 = new Vector2(value1.X, value1.Y);
                val2 = new Vector2(value2.X, value2.Y);
                return Vector2.CrossProduct(val1, val2);
            default:
                return 0;
        }
    }
    public static void Cross(ref Vector3 vector1, ref Vector3 vector2, out Vector3 result)
    {
        result = new Vector3(vector1.Y * vector2.Z - vector2.Y * vector1.Z,
                             -(vector1.X * vector2.Z - vector2.X * vector1.Z),
                             vector1.X * vector2.Y - vector2.X * vector1.Y);
    }

    public static double Distance(Vector3 vector1, Vector3 vector2)
    {
        double result;
        DistanceSquared(ref vector1, ref vector2, out result);
        return (double)Math.Sqrt(result);
    }

    public static void Distance(ref Vector3 value1, ref Vector3 value2, out double result)
    {
        DistanceSquared(ref value1, ref value2, out result);
        result = (double)Math.Sqrt(result);
    }

    public static double DistanceSquared(Vector3 value1, Vector3 value2)
    {
        double result;
        DistanceSquared(ref value1, ref value2, out result);
        return result;
    }

    public static void DistanceSquared(ref Vector3 value1, ref Vector3 value2, out double result)
    {
        result = (value1.X - value2.X) * (value1.X - value2.X) +
                 (value1.Y - value2.Y) * (value1.Y - value2.Y) +
                 (value1.Z - value2.Z) * (value1.Z - value2.Z);
    }

    public static Vector3 Direction(Vector3 value1, Vector3 value2)
    {
        Direction(ref value1, ref value2, out value1);
        return value1;
    }

    public static void Direction(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
    {
        result = Vector3.Normalize(value2 - value1);
    }

    public static Vector3 Divide(Vector3 value1, Vector3 value2)
    {
        value1.X /= value2.X;
        value1.Y /= value2.Y;
        value1.Z /= value2.Z;
        return value1;
    }

    public static Vector3 Divide(Vector3 value1, double value2)
    {
        double factor = 1 / value2;
        value1.X *= factor;
        value1.Y *= factor;
        value1.Z *= factor;
        return value1;
    }

    public static void Divide(ref Vector3 value1, double divisor, out Vector3 result)
    {
        double factor = 1 / divisor;
        result.X = value1.X * factor;
        result.Y = value1.Y * factor;
        result.Z = value1.Z * factor;
    }

    public static void Divide(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
    {
        result.X = value1.X / value2.X;
        result.Y = value1.Y / value2.Y;
        result.Z = value1.Z / value2.Z;
    }

    public static double Dot(Vector3 vector1, Vector3 vector2)
    {
        return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
    }

    public static void Dot(ref Vector3 vector1, ref Vector3 vector2, out double result)
    {
        result = vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
    }

    public override bool Equals(object obj)
    {
        return (obj is Vector3) ? this == (Vector3)obj : false;
    }

    public bool Equals(Vector3 other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return (int)(this.X + this.Y + this.Z);
    }

    public static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, double amount)
    {
        Vector3 result = new Vector3();
        Hermite(ref value1, ref tangent1, ref value2, ref tangent2, amount, out result);
        return result;
    }

    public static void Hermite(ref Vector3 value1, ref Vector3 tangent1, ref Vector3 value2, ref Vector3 tangent2, double amount, out Vector3 result)
    {
        result.X = MathHelper.Hermite(value1.X, tangent1.X, value2.X, tangent2.X, amount);
        result.Y = MathHelper.Hermite(value1.Y, tangent1.Y, value2.Y, tangent2.Y, amount);
        result.Z = MathHelper.Hermite(value1.Z, tangent1.Z, value2.Z, tangent2.Z, amount);
    }

    public bool IsNAN()
    {
        return IsNAN(this);
    }

    public static bool IsNAN(Vector3 value)
    {
        return (double.IsNaN(value.X) || double.IsNaN(value.Y) || double.IsNaN(value.Z));
    }
    public double Length()
    {
        var origin = zero;
        DistanceSquared(ref this, ref origin, out double result);
        return Math.Sqrt(result);
    }

    public double LengthSquared()
    {
        var origin = zero;
        DistanceSquared(ref this, ref origin, out double result);
        return result;
    }

    public static Vector3 Lerp(Vector3 value1, Vector3 value2, double amount)
    {
        return new Vector3(
            MathHelper.Lerp(value1.X, value2.X, amount),
            MathHelper.Lerp(value1.Y, value2.Y, amount),
            MathHelper.Lerp(value1.Z, value2.Z, amount));
    }

    public static void Lerp(ref Vector3 value1, ref Vector3 value2, double amount, out Vector3 result)
    {
        result = new Vector3(
            MathHelper.Lerp(value1.X, value2.X, amount),
            MathHelper.Lerp(value1.Y, value2.Y, amount),
            MathHelper.Lerp(value1.Z, value2.Z, amount));
    }

    public static Vector3 Max(Vector3 value1, Vector3 value2)
    {
        return new Vector3(
            MathHelper.Max(value1.X, value2.X),
            MathHelper.Max(value1.Y, value2.Y),
            MathHelper.Max(value1.Z, value2.Z));
    }

    public static void Max(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
    {
        result = new Vector3(
            MathHelper.Max(value1.X, value2.X),
            MathHelper.Max(value1.Y, value2.Y),
            MathHelper.Max(value1.Z, value2.Z));
    }

    public static Vector3 Min(Vector3 value1, Vector3 value2)
    {
        return new Vector3(
            MathHelper.Min(value1.X, value2.X),
            MathHelper.Min(value1.Y, value2.Y),
            MathHelper.Min(value1.Z, value2.Z));
    }

    public static void Min(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
    {
        result = new Vector3(
            MathHelper.Min(value1.X, value2.X),
            MathHelper.Min(value1.Y, value2.Y),
            MathHelper.Min(value1.Z, value2.Z));
    }

    public static Vector3 Multiply(Vector3 value1, Vector3 value2)
    {
        value1.X *= value2.X;
        value1.Y *= value2.Y;
        value1.Z *= value2.Z;
        return value1;
    }

    public static Vector3 Multiply(Vector3 value1, double scaleFactor)
    {
        value1.X *= scaleFactor;
        value1.Y *= scaleFactor;
        value1.Z *= scaleFactor;
        return value1;
    }

    public static void Multiply(ref Vector3 value1, double scaleFactor, out Vector3 result)
    {
        result.X = value1.X * scaleFactor;
        result.Y = value1.Y * scaleFactor;
        result.Z = value1.Z * scaleFactor;
    }

    public static void Multiply(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
    {
        result.X = value1.X * value2.X;
        result.Y = value1.Y * value2.Y;
        result.Z = value1.Z * value2.Z;
    }

    public static Vector3 Negate(Vector3 value)
    {
        value = new Vector3(-value.X, -value.Y, -value.Z);
        return value;
    }

    public static void Negate(ref Vector3 value, out Vector3 result)
    {
        result = new Vector3(-value.X, -value.Y, -value.Z);
    }

    public void Normalize()
    {
        Normalize(ref this, out this);
    }

    public static Vector3 Normalize(Vector3 vector)
    {
        Normalize(ref vector, out vector);
        return vector;
    }

    public static void Normalize(ref Vector3 value, out Vector3 result)
    {
        var origin = zero;
        Distance(ref value, ref origin, out double factor);
        factor = 1.0 / factor;
        result.X = value.X * factor;
        result.Y = value.Y * factor;
        result.Z = value.Z * factor;
    }

    public static Vector3 Reflect(Vector3 vector, Vector3 normal)
    {
        // I is the original array
        // N is the normal of the incident plane
        // R = I - (2 * N * ( DotProduct[ I,N] ))
        Vector3 reflectedVector;
        // inline the dotProduct here instead of calling method
        double dotProduct = ((vector.X * normal.X) + (vector.Y * normal.Y)) + (vector.Z * normal.Z);
        reflectedVector.X = vector.X - (2.0f * normal.X) * dotProduct;
        reflectedVector.Y = vector.Y - (2.0f * normal.Y) * dotProduct;
        reflectedVector.Z = vector.Z - (2.0f * normal.Z) * dotProduct;

        return reflectedVector;
    }

    public static void Reflect(ref Vector3 vector, ref Vector3 normal, out Vector3 result)
    {
        // I is the original array
        // N is the normal of the incident plane
        // R = I - (2 * N * ( DotProduct[ I,N] ))

        // inline the dotProduct here instead of calling method
        double dotProduct = ((vector.X * normal.X) + (vector.Y * normal.Y)) + (vector.Z * normal.Z);
        result.X = vector.X - (2.0f * normal.X) * dotProduct;
        result.Y = vector.Y - (2.0f * normal.Y) * dotProduct;
        result.Z = vector.Z - (2.0f * normal.Z) * dotProduct;

    }

    public static Vector3 SmoothStep(Vector3 value1, Vector3 value2, double amount)
    {
        return new Vector3(
            MathHelper.SmoothStep(value1.X, value2.X, amount),
            MathHelper.SmoothStep(value1.Y, value2.Y, amount),
            MathHelper.SmoothStep(value1.Z, value2.Z, amount));
    }

    public static void SmoothStep(ref Vector3 value1, ref Vector3 value2, double amount, out Vector3 result)
    {
        result = new Vector3(
            MathHelper.SmoothStep(value1.X, value2.X, amount),
            MathHelper.SmoothStep(value1.Y, value2.Y, amount),
            MathHelper.SmoothStep(value1.Z, value2.Z, amount));
    }

    public static Vector3 Subtract(Vector3 value1, Vector3 value2)
    {
        value1.X -= value2.X;
        value1.Y -= value2.Y;
        value1.Z -= value2.Z;
        return value1;
    }

    public static void Subtract(ref Vector3 value1, ref Vector3 value2, out Vector3 result)
    {
        result.X = value1.X - value2.X;
        result.Y = value1.Y - value2.Y;
        result.Z = value1.Z - value2.Z;
    }

    public override string ToString() => $"{{X:{X:0.#}/ Y:{Y:0.#}/ Z:{Z:0.#}}}";

    public static Vector3 Transform(Vector3 position, Matrix matrix)
    {
        Transform(ref position, ref matrix, out position);
        return position;
    }

    public static void Transform(ref Vector3 position, ref Matrix matrix, out Vector3 result)
    {
        result = new Vector3((position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
                             (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
                             (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43);
    }

    public static void Transform(Vector3[] sourceArray, ref Matrix matrix, Vector3[] destinationArray)
    {
        Debug.Assert(destinationArray.Length >= sourceArray.Length, "The destination array is smaller than the source array.");

        // TODO: Are there options on some platforms to implement a vectorized version of this?

        for (var i = 0; i < sourceArray.Length; i++)
        {
            var position = sourceArray[i];
            destinationArray[i] =
                new Vector3(
                    (position.X * matrix.M11) + (position.Y * matrix.M21) + (position.Z * matrix.M31) + matrix.M41,
                    (position.X * matrix.M12) + (position.Y * matrix.M22) + (position.Z * matrix.M32) + matrix.M42,
                    (position.X * matrix.M13) + (position.Y * matrix.M23) + (position.Z * matrix.M33) + matrix.M43);
        }
    }

    /// <summary>
    /// Transforms a vector by a quaternion rotation.
    /// </summary>
    /// <param name="vec">The vector to transform.</param>
    /// <param name="quat">The quaternion to rotate the vector by.</param>
    /// <returns>The result of the operation.</returns>
    public static Vector3 Transform(Vector3 vec, Quaternion quat)
    {
        Vector3 result;
        Transform(ref vec, ref quat, out result);
        return result;
    }

    /// <summary>
    /// Transforms a vector by a quaternion rotation.
    /// </summary>
    /// <param name="vec">The vector to transform.</param>
    /// <param name="quat">The quaternion to rotate the vector by.</param>
    /// <param name="result">The result of the operation.</param>
    //        public static void Transform(ref Vector3 vec, ref Quaternion quat, out Vector3 result)
    //        {
    //		// Taken from the OpentTK implementation of Vector3
    //            // Since vec.W == 0, we can optimize quat * vec * quat^-1 as follows:
    //            // vec + 2.0 * cross(quat.xyz, cross(quat.xyz, vec) + quat.w * vec)
    //            Vector3 xyz = quat.Xyz, temp, temp2;
    //            Vector3.Cross(ref xyz, ref vec, out temp);
    //            Vector3.Multiply(ref vec, quat.W, out temp2);
    //            Vector3.Add(ref temp, ref temp2, out temp);
    //            Vector3.Cross(ref xyz, ref temp, out temp);
    //            Vector3.Multiply(ref temp, 2, out temp);
    //            Vector3.Add(ref vec, ref temp, out result);
    //        }

    /// <summary>
    /// Transforms a vector by a quaternion rotation.
    /// </summary>
    /// <param name="vec">The vector to transform.</param>
    /// <param name="quat">The quaternion to rotate the vector by.</param>
    /// <param name="result">The result of the operation.</param>
    public static void Transform(ref Vector3 vec, ref Quaternion quat, out Vector3 result)
    {
        // This has not been tested
        // TODO:  This could probably be unrolled so will look into it later
        Matrix matrix = quat.ToMatrix();
        Transform(ref vec, ref matrix, out result);
    }

    public static Vector3 TransformNormal(Vector3 normal, Matrix matrix)
    {
        TransformNormal(ref normal, ref matrix, out normal);
        return normal;
    }

    public static void TransformNormal(ref Vector3 normal, ref Matrix matrix, out Vector3 result)
    {
        result = new Vector3((normal.X * matrix.M11) + (normal.Y * matrix.M21) + (normal.Z * matrix.M31),
                             (normal.X * matrix.M12) + (normal.Y * matrix.M22) + (normal.Z * matrix.M32),
                             (normal.X * matrix.M13) + (normal.Y * matrix.M23) + (normal.Z * matrix.M33));
    }

    public void Round()
    {
        X = Math.Round(X);
        Y = Math.Round(Y);
        Z = Math.Round(Z);
    }

    public static Vector3 RotateByDegree(Vector3 vec, double degree, Coordinate standard)
    {
        return RotateByRadian(vec, degree * Math.PI / 180, standard);
    }

    public static Vector3 RotateByRadian(Vector3 vec, double radian, Coordinate standard)
    {
        Vector2 vec2 = new Vector2(0, 0);
        switch (standard)
        {
            case Coordinate.X:
                vec2 = new Vector2(vec.Y, vec.Z);
                break;
            case Coordinate.Y:
                vec2 = new Vector2(vec.X, vec.Z);
                break;
            case Coordinate.Z:
                vec2 = new Vector2(vec.X, vec.Y);
                break;
        }

        if (vec2.Length > 0)
        {
            vec2 = vec2.RotateByRadian(radian);
            Vector3 temp = new Vector3(vec);
            switch (standard)
            {
                case Coordinate.X:
                    temp.Y = vec2.X;
                    temp.Z = vec2.Y;
                    break;
                case Coordinate.Y:
                    temp.X = vec2.X;
                    temp.Z = vec2.Y;
                    break;
                case Coordinate.Z:
                    temp.X = vec2.X;
                    temp.Y = vec2.Y;
                    break;
            }
            return temp;
        }
        else
        {
            return new Vector3(0, 0, 0);
        }
    }
    #endregion Public methods

    #region Operators

    public static bool operator ==(Vector3 value1, Vector3 value2)
    {
        var difference = value1 - value2;
        return Math.Abs(difference.X) < 0.001
            && Math.Abs(difference.Y) < 0.001
            && Math.Abs(difference.Z) < 0.001;
    }

    public static bool operator !=(Vector3 value1, Vector3 value2)
    {
        return !(value1 == value2);
    }

    public static Vector3 operator +(Vector3 value1, Vector3 value2)
    {
        value1.X += value2.X;
        value1.Y += value2.Y;
        value1.Z += value2.Z;
        return value1;
    }

    public static Vector3 operator -(Vector3 value)
    {
        value = new Vector3(-value.X, -value.Y, -value.Z);
        return value;
    }

    public static Vector3 operator -(Vector3 value1, Vector3 value2)
    {
        value1.X -= value2.X;
        value1.Y -= value2.Y;
        value1.Z -= value2.Z;
        return value1;
    }

    public static Vector3 operator *(Vector3 value1, Vector3 value2)
    {
        value1.X *= value2.X;
        value1.Y *= value2.Y;
        value1.Z *= value2.Z;
        return value1;
    }

    public static Vector3 operator *(Vector3 value, double scaleFactor)
    {
        value.X *= scaleFactor;
        value.Y *= scaleFactor;
        value.Z *= scaleFactor;
        return value;
    }

    public static Vector3 operator *(double scaleFactor, Vector3 value)
    {
        value.X *= scaleFactor;
        value.Y *= scaleFactor;
        value.Z *= scaleFactor;
        return value;
    }

    public static Vector3 operator /(Vector3 value1, Vector3 value2)
    {
        value1.X /= value2.X;
        value1.Y /= value2.Y;
        value1.Z /= value2.Z;
        return value1;
    }

    public static Vector3 operator /(Vector3 value, double divider)
    {
        double factor = 1 / divider;
        value.X *= factor;
        value.Y *= factor;
        value.Z *= factor;
        return value;
    }

    #endregion
}
