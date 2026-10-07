namespace TI5X.Avalonia.Core;

public static class Arith
{
    public const int MaxExponent = 99;
    public const int MinExponent = -99;

    public const int ANG_DEG = 0;
    public const int ANG_RAD = 1;
    public const int ANG_GRAD = 2;

    public const int FORMAT_FIXED = 0;
    public const int FORMAT_FLOAT = 1;
    public const int FORMAT_ENG = 2;

    public static int PowerOf10(double val)
    {
        if (val == 0.0)
            return 0;
        double logVal = Math.Log10(Math.Abs(val));
        return (int)Math.Floor(logVal);
    }

    public static int FiguresBeforeDecimal(double val, int exp)
    {
        if (val == 0.0)
            return 1;
        double scaled = Math.Abs(val) / Math.Pow(10.0, exp);
        if (scaled < 1.0)
            return 1;
        return Math.Max(1, (int)Math.Floor(Math.Log10(scaled)) + 1);
    }

    public static double RoundTo(double val, int decimals)
    {
        if (double.IsNaN(val) || double.IsInfinity(val) || val == 0.0)
            return val;

        int exp = PowerOf10(val);
        int scale = decimals - exp - 1;
        double factor = Math.Pow(10.0, scale);
        return Math.Round(val * factor, MidpointRounding.AwayFromZero) / factor;
    }

    public static double DegToRad(double deg) => deg * (Math.PI / 180.0);
    public static double RadToDeg(double rad) => rad * (180.0 / Math.PI);
    public static double GradToRad(double grad) => grad * (Math.PI / 200.0);
    public static double RadToGrad(double rad) => rad * (200.0 / Math.PI);

    public static double ToRadians(double angle, int angleMode)
    {
        return angleMode switch
        {
            ANG_DEG => DegToRad(angle),
            ANG_GRAD => GradToRad(angle),
            _ => angle
        };
    }

    public static double FromRadians(double rad, int angleMode)
    {
        return angleMode switch
        {
            ANG_DEG => RadToDeg(rad),
            ANG_GRAD => RadToGrad(rad),
            _ => rad
        };
    }

    /// <summary>
    /// Converts decimal degrees to D.MS (Degrees.MinutesSecondsFraction).
    /// </summary>
    public static double ToDms(double deg)
    {
        double sign = deg < 0 ? -1.0 : 1.0;
        deg = Math.Abs(deg);
        double d = Math.Floor(deg);
        double rem = (deg - d) * 60.0;
        double m = Math.Floor(rem);
        double s = (rem - m) * 60.0;
        return sign * (d + m / 100.0 + s / 10000.0);
    }

    /// <summary>
    /// Converts D.MS (Degrees.MinutesSecondsFraction) to decimal degrees.
    /// </summary>
    public static double FromDms(double dms)
    {
        double sign = dms < 0 ? -1.0 : 1.0;
        dms = Math.Abs(dms);
        double d = Math.Floor(dms);
        double rem = (dms - d) * 100.0;
        double m = Math.Floor(rem);
        double s = (rem - m) * 100.0;
        return sign * (d + m / 60.0 + s / 3600.0);
    }

    /// <summary>
    /// Polar (r, theta) to Rectangular (x, y).
    /// Input: x = r, t = theta. Output: x = x-coord, t = y-coord.
    /// </summary>
    public static (double x, double y) PolarToRect(double r, double theta, int angleMode)
    {
        double rad = ToRadians(theta, angleMode);
        double x = r * Math.Cos(rad);
        double y = r * Math.Sin(rad);
        return (x, y);
    }

    /// <summary>
    /// Rectangular (x, y) to Polar (r, theta).
    /// Input: x = x-coord, t = y-coord. Output: x = r, t = theta.
    /// </summary>
    public static (double r, double theta) RectToPolar(double x, double y, int angleMode)
    {
        double r = Math.Sqrt(x * x + y * y);
        double rad = Math.Atan2(y, x);
        double theta = FromRadians(rad, angleMode);
        return (r, theta);
    }
}
