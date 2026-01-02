namespace TriUgla.Mesher
{
    public readonly struct Edge(
        int start, int end,
        int triangle = -1, string? id = null)
    {
        public readonly int start = start, end = end;
        public readonly int triangle = triangle;
        public readonly string? id;

        public readonly static void Split(
            in Edge edge, 
            int vertex, 
            int triangle1,
            int triangle2,
            out Edge edge1,
            out Edge edge2)
        {
            edge1 = new Edge(
                edge.start, 
                vertex, 
                triangle1,
                edge.id);

            edge2 = new Edge(
                vertex,
                edge.end,
                triangle2,
                edge.id);
        }
    }
}
