namespace TriUgla.Mesher
{
    public struct EdgeMeta(int triangle, string? id = null)
    {
        public string? id = id;
        public int triangle = triangle;
    }
}
