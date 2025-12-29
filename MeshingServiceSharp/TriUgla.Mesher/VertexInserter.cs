using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class VertexInserter(Mesh mesh)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Triangle> Triangles() => CollectionsMarshal.AsSpan(mesh.Triangles);

        public bool SetConstraint(Mesh mesh, int start, int end, string? id)
        {
            ReadOnlySpan<Triangle> tris = Triangles();
            int seed = mesh.Vertices.Meta[start].triangle;
            Circler circler = new Circler(tris, seed, start);
            do
            {
                int ti = circler.CurrentTriangle;
                Triangle t = tris[ti];
                int edge = Triangle.IndexOfInvariant(in t, start, end);
                if (edge != -1)
                {
                    AddConstraint(ti, edge, id);
                    return true;
                }

            } while (circler.Next());

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddConstraint(int triangle, int edge, string? id)
        {
            if ((uint)edge > 2u) throw new ArgumentOutOfRangeException(nameof(edge));

            List<Triangle> tris = mesh.Triangles;
            List<Edge> edges = mesh.Edges.Items;
            List<EdgeMeta> metas = mesh.Edges.Meta;

            int baseId = edges.Count;
            Triangle tri = tris[triangle];

            int start, end, adjacent;
            switch (edge)
            {
                case 0:
                    tri.con0 = baseId;
                    adjacent = tri.adj0;
                    start = tri.vtx0;
                    end = tri.vtx1;
                    break;

                case 1:
                    tri.con1 = baseId;
                    adjacent = tri.adj1;
                    start = tri.vtx1;
                    end = tri.vtx2;
                    break;

                default:
                    tri.con2 = baseId;
                    adjacent = tri.adj2;
                    start = tri.vtx2;
                    end = tri.vtx0;
                    break;
            }

            edges.Add(new Edge(start, end));
            metas.Add(new EdgeMeta(triangle, id));
            tris[triangle] = tri;

            if (adjacent == -1)
                return;

            Triangle triAdj = tris[adjacent];

            int otherEdge = Triangle.IndexOf(in triAdj, end, start);
            if ((uint)otherEdge > 2u)
                throw new InvalidOperationException("Adjacent triangle does not contain the shared edge (topology broken).");

            int backId = baseId + 1;
            switch (otherEdge)
            {
                case 0: triAdj.con0 = backId; break;
                case 1: triAdj.con1 = backId; break;
                default: triAdj.con2 = backId; break;
            }

            edges.Add(new Edge(end, start));
            metas.Add(new EdgeMeta(adjacent, id));
            tris[adjacent] = triAdj;
        }
    }
}
