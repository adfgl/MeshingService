namespace MeshingServiceLib
{
    public struct TriangleEdge(string? id, int start, int end, int triangle)
    {
        public readonly string? id = id;
        public readonly int start = start, end = end;
        public int triangle = triangle;

    }
}
