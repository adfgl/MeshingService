public sealed class DtoGeometry
{
    public DtoPolygon Contour { get; set; }
    public List<DtoPolygon>? Holes { get; set; }
    public List<DtoConstrainedEdge>? ConstrainedEdges { get; set; }
    public List<DtoVertex>? ConstrainedPoints { get; set; }
}

public sealed class DtoPolygon
{
    public string? Id { get; set; }
    public List<DtoVertex> Vertices { get; set; }
}

public sealed class DtoConstrainedEdge
{
    public string? Id { get; set; }
    public DtoVertex Start { get; set; }
    public DtoVertex End { get; set; }
}