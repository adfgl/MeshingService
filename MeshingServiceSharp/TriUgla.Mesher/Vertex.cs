namespace TriUgla.Mesher
{
    public struct Vertex(
        double x, double y, double z, 
        int triangle = -1, string? id = null)
    {
        public readonly double x = x, y = y, z = z;
        public triangle = triangle;
        public string? id = id;

        public void Deconstruct(out double x, out double y)
        {
            x = this.x; y = this.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Close(in Vertex a, in Vertex b, double eps)
        {
            double dx = a.x - b.x, dy = a.y - b.y;
            return dx * dx + dy * dy <= eps * eps;
        }

        public static Vertex Between(in Vertex a, in Vertex b)
            => new (
                (a.x + b.x) * 0.5,
                (a.y + b.y) * 0.5,
                (a.z + b.z) * 0.5);
    }
}
