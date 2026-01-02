namespace TriUgla.Mesher
{
    public struct VertexMeta(int triangle, string? id = null)
    {
        public string? id = id;
        public int triangle = triangle;
    }
}
