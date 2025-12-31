public sealed class DtoEdge
{
    public string? Id { get; set; }
    public int Vertex { get; set; } = -1;
    public int Triangle { get; set; } = -1;
    public int Next { get; set; } = -1;
    public int Previous { get; set; } = -1;
    public int Twin { get; set; } = -1;
}