namespace TriUgla.Mesher
{
    public readonly struct Vertex(double x, double y)
    {
        public readonly double x = x, y = y;

        public void Deconstruct(out double x, out double y)
        {
            x = this.x; y = this.y;
        }

        public static bool Close(in Vertex a, in Vertex b, double eps)
        {
            double dx = a.x - b.x, dy = a.y - b.y;
            return dx * dx + dy * dy <= eps * eps;
        }
    }
}
