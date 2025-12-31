public sealed class DtoEdge
{
    public string? Id { get; set; }
    public int Vertex { get; set; }
    public int Triangle { get; set; }
    public int Next { get; set; }
    public int Previous { get; set; }
}