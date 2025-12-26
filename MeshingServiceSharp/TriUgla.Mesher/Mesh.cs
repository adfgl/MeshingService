namespace TriUgla.Mesher
{
    public sealed class Mesh
    {
        public List<Triangle> Triangles { get; } = new List<Triangle>();
        public List<Circle> Circles { get; } = new List<Circle>();
        public Edges Edges { get; } = new Edges();
        public Vertices Vertices { get; } = new Vertices();
    }

    public sealed class Edges
    {
        public List<Edge> Items { get; } = new List<Edge>();
        public List<EdgeMeta> Meta { get; } = new List<EdgeMeta>();
    }

    public sealed class Vertices
    {
        public List<Vertex> Items { get; } = new List<Vertex>();    
        public List<VertexMeta> Meta {  get; } = new List<VertexMeta>();
    }
}
