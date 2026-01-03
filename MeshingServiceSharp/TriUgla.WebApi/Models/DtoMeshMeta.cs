namespace TriUgla.WebApi.Models
{
    public sealed class DtoMeshMeta
    {
        public DtoMeshMetric EdgeLength { get; set; }
        public DtoMeshMetric TriangleArea { get; set; }
        public DtoMeshMetric TriangleAngle { get; set; }
    }
}
