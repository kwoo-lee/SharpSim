namespace SharpSim;
public static class SweepingVolume
{
    private static double _width = 0;
    private static double _depth = 0;
    private static double _diagonalLength = 0;
    public static double Width => _width;
    public static double Depth => _depth;
    public static double DiagonalLength => _diagonalLength;

    public static double LongerLength => _width > _depth ? _width : _depth;
    public static void SetSize(double width, double depth)
    {
        _width = width;
        _depth = depth;
        _diagonalLength = Math.Sqrt((_width / 2) * (_width / 2) + (_depth / 2) * (_depth / 2));
    }

    public static Polygon FindVolumeByPose(Vector2 centerPos, Vector2 pose)
    {
        // 직사각형 Shape 기준
        var center = new Vector2(centerPos.X, centerPos.Y);
        var rbPt = center + new Vector2(Width / 2, -Depth / 2); // Right&Bottom Point
        var rtPt = center + new Vector2(Width / 2, Depth / 2); // Right&Top Point
        var lbPt = center + new Vector2(-Width / 2, -Depth / 2); // Left&Bottom Point
        var ltPt = center + new Vector2(-Width / 2, Depth / 2); // Left&Top Point
        var polygon = new Polygon(new List<Vector2>() { rbPt, rtPt, lbPt, ltPt });

        var rad = pose.AbsoluteAngleRadian();
        polygon.RotateByRadian(centerPos, pose.AbsoluteAngleRadian());
        return polygon;
    }
}
