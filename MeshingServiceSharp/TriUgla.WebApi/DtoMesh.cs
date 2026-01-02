namespace TriUgla.WebApi
{
    public sealed class DtoMesh
    {
        public List<DtoVertex> Vertices { get; set; }
        public List<DtoEdge> Edges { get; set; }
        public List<DtoFace> Faces { get; set; }
        public DtoMeshMeta Meta { get; set; }
    }

    public sealed class DtoMeshGreedy
    {
        public double[] X { get; set; }
        public double[] Y { get; set; }
        public int[] Indices3 { get; set; }
    }

    public sealed class DtoMeshMeta
    {
        public DtoMeshMetric EdgeLength { get; set; }
        public DtoMeshMetric TriangleArea { get; set; }
        public DtoMeshMetric TriangleAngle { get; set; }
    }

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