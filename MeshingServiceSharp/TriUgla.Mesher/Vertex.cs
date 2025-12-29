namespace TriUgla.Mesher
{
    public readonly struct Vertex(double x, double y) : IEquatable<Vertex>
    {
        public readonly double x = x, y = y;

        public void Deconstruct(out double x, out double y)
        {
            x = this.x; y = this.y;
        }

        public bool Equals(Vertex other) => x == other.x && y == other.y;
    }
}
