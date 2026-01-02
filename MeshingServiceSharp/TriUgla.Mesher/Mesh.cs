using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class Mesh
    {
        public List<Triangle> Triangles { get; } = new List<Triangle>();
        public List<Circle> Circles { get; } = new List<Circle>();
        public List<Edge> Edges { get; } = new List<Edge>();
        public Vertices Vertices { get; } = new Vertices();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Triangle> TrianglesSpan() => CollectionsMarshal.AsSpan(Triangles);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Vertex> VerticesSpan() => CollectionsMarshal.AsSpan(Vertices.Items);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<VertexMeta> VertexMetaSpan() => CollectionsMarshal.AsSpan(Vertices.Meta);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<Circle> CirclesSpan() => CollectionsMarshal.AsSpan(Circles);

    }

    public sealed class Vertices
    {
        public List<Vertex> Items { get; } = new List<Vertex>();    
        public List<VertexMeta> Meta {  get; } = new List<VertexMeta>();
    }
}
