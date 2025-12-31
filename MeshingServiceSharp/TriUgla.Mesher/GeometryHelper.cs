using System.Runtime.CompilerServices;

namespace TriUgla.Mesher
{
    public static class GeometryHelper
    {
        public static double Interpolate(
            double ax, double ay, double a, 
            double bx, double by, double b,
            double x, double y)
        {
            double dx = bx - ax, dy = by - ay;
            double len2 = dx * dx + dy * dy;
            if (len2 <= double.Epsilon)
            {
                return 0.5 * (a + b);
            }

            double t = ((x - ax) * dx + (y - ay) * dy) / len2;
            if (t < 0)
            {
                t = 0;
            }
            else if (t > 1)
            {
                t = 1;
            }
            return a + t * (b - a);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(
            double ax, double ay,
            double bx, double by,
            double cx, double cy)
        {
            return (bx - ax) * (cy - ay) - (by - ay) * (cx - ax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreCollinear(
            double ax, double ay,
            double bx, double by,
            double cx, double cy,
            double eps)
        {
            return Math.Abs(Cross(ax, ay, bx, by, cx, cy)) <= eps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsConvex(
            double ax, double ay,
            double bx, double by,
            double cx, double cy,
            double dx, double dy)
        {
            return
                Cross(ax, ay, bx, by, cx, cy) > 0 &&
                Cross(bx, by, cx, cy, dx, dy) > 0 &&
                Cross(cx, cy, dx, dy, ax, ay) > 0 &&
                Cross(dx, dy, ax, ay, bx, by) > 0;
        }

        public static bool PointOnSegment(
            double ax, double ay, 
            double bx, double by, 
            double x, double y, 
            double eps)
        {
            if (x < Math.Min(ax, bx) - eps || x > Math.Max(ax, bx) + eps ||
                y < Math.Min(ay, by) - eps || y > Math.Max(ay, by) + eps)
                return false;

            double dx = bx - ax;
            double dy = by - ay;

            double dxp = x - ax;
            double dyp = y - ay;

            double cross = dx * dyp - dy * dxp;
            if (Math.Abs(cross) > eps)
                return false;

            double dot = dx * dx + dy * dy;
            if (dot < eps)
            {
                double ddx = ax - x;
                double ddy = ay - y;
                return ddx * ddx + ddy * ddy <= eps;
            }

            double t = (dxp * dx + dyp * dy) / dot;
            return t >= -eps && t <= 1 + eps;
        }
            

        public readonly static bool Intersect(
            double p1x, double p1y,
            double p2x, double p2y,
            double q1x, double q1y,
            double q2x, double q2y,
            out double x, out double y)
        {
            // P(u) = p1 + u * (p2 - p1)
            // Q(v) = q1 + v * (q2 - q1)

            // goal to vind such 'u' and 'v' so:
            // p1 + u * (p2 - p1) = q1 + v * (q2 - q1)
            // which is:
            // u * (p2x - p1x) - v * (q2x - q1x) = q1x - p1x
            // u * (p2y - p1y) - v * (q2y - q1y) = q1y - p1y

            // | p2x - p1x  -(q2x - q1x) | *  | u | =  | q1x - p1x |
            // | p2y - p1y  -(q2y - q1y) |    | v |    | q1y - p1y |

            // | a  b | * | u | = | e |
            // | c  d |   | v |   | f |

            x = y = Double.NaN;

            double a = p2x - p1x, b = q1x - q2x;
            double c = p2y - p1y, d = q1y - q2y;

            double det = a * d - b * c;
            if (Math.Abs(det) < 1e-12)
            {
                return false;
            }

            double e = q1x - p1x, f = q1y - p1y;
            double u = (e * d - b * f) / det;
            double v = (a * f - e * c) / det;

            if (u < 0 || u > 1 || v < 0 || v > 1)
            {
                return false;
            }

            x = p1x + u * a;
            y = p1y + u * c;
            return true;
        }
    }
}
