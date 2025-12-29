namespace TriUgla.Mesher
{
    public readonly struct Vertex(double x, double y)
    {
        public readonly double x = x, y = y;

        public void Deconstruct(out double x, out double y)
        {
            x = this.x; y = this.y;
        }
    }
}
