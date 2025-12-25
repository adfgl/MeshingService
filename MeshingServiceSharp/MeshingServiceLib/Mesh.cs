namespace MeshingServiceLib
{
    public sealed class Mesh
    {
        public List<Vertex> Vertices { get; set; }
        public List<Circle> Circles { get; set; }
        public List<Triangle> Triangles { get; set; }
    }
}
