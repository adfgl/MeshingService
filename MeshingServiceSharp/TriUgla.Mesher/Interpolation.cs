using System.Runtime.CompilerServices;

namespace TriUgla.Mesher
{
    public static class Interpolation
    {
        public static double ZAtXYAlongSegment(
            in Vertex v0, in Vertex v1, double x, double y)
        {
            double dx = v1.x - v0.x;
            double dy = v1.y - v0.y;

            double ddx = v1.x - x;
            double ddy = v1.y - y;

            double k = Math.Sqrt(
                (ddx * ddx + ddy * ddy) /
                (dx * dx + dy * dy));

            return v0.z + (v1.z - v0.z) * k;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(double ax, double ay, double bx, double by)
        {
            return ax * by - ay * bx;
        }

        public static double ZAtXYInTriangle(
            in Vertex v0, in Vertex v1, Vertex v2,
            double x, double y)
        {
            double e0x = v1.x - v0.x;
            double e0y = v1.y - v0.y;

            double e1x = v2.x - v0.x;
            double e1y = v2.y - v0.y;

            double px = x - v0.x;
            double py = y - v0.y;

            double denom = Cross(e0x, e0y, e1x, e1y);

#if DEBUG
            if (Math.Abs(denom) < 1e-30)
                throw new InvalidOperationException("Triangle is degenerate in XY.");
#endif

            double w1 = Cross(px, py, e1x, e1y) / denom; // weight at v1
            double w2 = Cross(e0x, e0y, px, py) / denom; // weight at v2
            double w0 = 1.0 - w1 - w2;

            return w0 * v0.z + w1 * v1.z + w2 * v2.z;
        }

    }
}