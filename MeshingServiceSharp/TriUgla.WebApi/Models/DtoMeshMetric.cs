namespace TriUgla.WebApi.Models
{
    public sealed class DtoMeshMetric
    {
        public int Quantity { get; set; }
        public double Min { get; set; }
        public int MinIndex { get; set; }
        public double Max { get; set; }
        public int MaxIndex { get; set; }
        public double Average { get; set; }
    }
}
