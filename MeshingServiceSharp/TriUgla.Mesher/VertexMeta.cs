namespace TriUgla.Mesher
{
    public struct VertexMeta(int triangle, string? id = null, double seed = -1)
    {
        public string? id = id;
        public int triangle = triangle;
        public double seed = seed;
    }
}
