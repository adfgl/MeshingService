namespace TriUgla.Mesher
{
    public readonly struct Edge(
        int start, int end,
        int triangle = -1, string? id = null)
    {
        public readonly int start = start, end = end;
        public readonly int triangle = triangle;
        public readonly string? id = id;

    }
}
