namespace TriUgla.WebApi.Models
{
    public sealed class DtoFace
    {
        public string? Id { get; set; }
        public DtoEdge[] Edges { get; set; }
        public double Area { get; set; }
    }
}