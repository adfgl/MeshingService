public sealed class DtoVertex
{
    public string? Id { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Seed { get; set; }
    public int Edge { get; set; } = -1;
    public int Triangle { get; set; } = -1;
}