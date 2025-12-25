namespace MeshingServiceLib
{
    public sealed class Mesh
    {
        public List<Vertex> Vertices { get; set; } = new List<Vertex>();
        public List<Circle> Circles { get; set; } = new List<Circle>();
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();
        public List<TriangleEdge> Edges { get; set; } = new List<TriangleEdge>();
    }
}
