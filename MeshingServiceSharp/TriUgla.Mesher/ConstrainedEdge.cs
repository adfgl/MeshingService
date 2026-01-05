public struct ConstrainedEdge(Vertex start, Vertex end, string? id)
{
    public readonly Vertex start = start, end = end;
    public string? id = id;
}