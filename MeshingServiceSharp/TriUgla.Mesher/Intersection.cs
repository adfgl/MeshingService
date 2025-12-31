public static class Intersection
{
    public readonly static bool Intersect(
        Vertex p1, Vertex p2,
        Vertex q1, Vertex q2,
        out Vertex intersection)
    {
        if (Intersect(
            p1.x, p1.y,
            p2.x, p2.y,
            out double x, out double y))
        {
            
        }
        intersection = default;
        return false;
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