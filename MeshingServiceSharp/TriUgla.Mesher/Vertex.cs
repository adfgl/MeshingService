namespace TriUgla.Mesher
{
    public readonly struct Vertex(double x, double y, double z)
    {
        public readonly double x = x, y = y, z = z;

        public void Deconstruct(out double x, out double y)
        {
            x = this.x; y = this.y;
        }

        public static bool Close(in Vertex a, in Vertex b, double eps)
        {
            double dx = a.x - b.x, dy = a.y - b.y;
            return dx * dx + dy * dy <= eps * eps;
        }

        public static readonly double ZAlongLineAtXY(
            Vertex v0, Vertex v1, double x, double y)
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
    }
}
