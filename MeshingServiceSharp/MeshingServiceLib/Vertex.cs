using System.Xml.Linq;

namespace MeshingServiceLib
{
    public sealed class Vertex(string? id, double x, double y, double seed) : IPoint
    {
        public string? Id { get; set; } = id;
        public double X { get; set; } = x;
        public double Y { get; set; } = y;
        public double Seed { get; set; } = seed;

        internal int Triangle { get; set; } = -1;

        public static double Interpolate(Vertex a, Vertex b, double x, double y)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y;
            double len2 = dx * dx + dy * dy;
            if (len2 <= double.Epsilon)
            {
                return 0.5 * (a.Seed + b.Seed);
            }

            double t = ((x - a.X) * dx + (y - a.Y) * dy) / len2;
            if (t < 0) t = 0; else if (t > 1) t = 1;
            return a.Seed + t * (b.Seed - a.Seed);
        }

        public static bool Close(Vertex a, Vertex b, double eps)
        {
            return GeometryHelper.LengthSquared(a, b) <= eps * eps;
        }

        public static Vertex Between(Vertex a, Vertex b)
        {
            return new Vertex(
                null, 
                (a.X + b.X) / 2.0, 
                (a.Y + b.Y) / 2.0, 
                (a.Seed + b.Seed) / 2.0);
        }

        public static Vertex Between(Vertex a, Vertex b, Vertex c)
        {
            return new Vertex(
                null,
                (a.X + b.X + c.X) / 3.0,
                (a.Y + b.Y + c.Y) / 3.0,
                (a.Seed + b.Seed + c.Seed) / 3.0);
        }
    }
}
