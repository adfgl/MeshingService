using System.Runtime.CompilerServices;

namespace TriUgla.Mesher
{
    public static class EdgeFlipping
    {
        public static bool CanFlip(this Mesh mesh, int triangleIndex, int edgeIndex, out bool should)
        {
            should = false;

            Span<Triangle> tris = mesh.TrianglesSpan();
            Span<Vertex> verts = mesh.VerticesSpan();

            Triangle t0 = mesh.Triangles[triangleIndex];
            if (edgeIndex != 0)
            {
                t0 = t0.Orient(edgeIndex);
            }

            if (!CanFlipFirstEdge(in t0))
            {
                return false;
            }

            ref readonly Triangle adjacent = ref tris[t0.adj0];
            ref readonly Vertex opposite = ref verts[Triangle.VertexIndexOppositeToEdge(in adjacent, t0.vtx1, t0.vtx0)];
            if (!GeometryHelper.IsConvex(
                    in verts[t0.vtx0],
                    in opposite,
                    in verts[t0.vtx1],
                    in verts[t0.vtx2]))
            {
                return false;
            }

            should = mesh.Circles[triangleIndex].Contains(in opposite);
            return true;
        }

        public static int FlipCCW(this Mesh mesh, Span<int> newTris, int triangleIndex, int edgeIndex)
        {
            /* 
            *        v2                   v2
            *        /\                  /|\
            *       /  \                / | \
            *      /    \              /  |  \
            *     /  t0  \            /   |   \
            *    /   →    \          /    |    \
            * v0+----------+v1    v0+  t0 | t1  +v1
            *    \   ←    /          \    |    /
            *     \  t1  /            \   |   /
            *      \    /              \  |  /
            *       \  /                \ | /
            *        \/                  \|/
            *        v3                   v3
            */

            Span<Triangle> tris = mesh.TrianglesSpan();
            Span<Circle> crcs = mesh.CirclesSpan();

            Triangle t0 = tris[triangleIndex];
            if (edgeIndex != 0)
            {
                t0 = t0.Orient(edgeIndex);
            }

            if (!CanFlipFirstEdge(in t0))
            {
                return -1;
            }

            int adjacentIndex = t0.adj0;
            Triangle t1 = tris[adjacentIndex];
            int e1 = Triangle.IndexOf(in t1, t0.vtx1, t0.vtx0);
            if (e1 != 0)
            {
                t1 = t1.Orient(e1);
            }

            int i0 = t0.vtx0;
            int i1 = t0.vtx1;
            int i2 = t0.vtx2;
            int i3 = t1.vtx2;

            Span<Vertex> v = mesh.VerticesSpan();

            // New diagonal is (i2 <-> i3)
            // t0 becomes (i0, i3, i2)
            crcs[triangleIndex] = Circle.From3Points(in v[i0], in v[i3], in v[i2]);
            tris[triangleIndex] = new Triangle(i0, i3, i2,
                t1.adj1, adjacentIndex, t0.adj2,
                t1.con1, -1, t0.con2);

            // t1 becomes (i3, i1, i2)
            crcs[adjacentIndex] = Circle.From3Points(in v[i3], in v[i1], in v[i2]);
            tris[adjacentIndex] = new Triangle(i3, i1, i2,
                t1.adj2, t0.adj1, triangleIndex,
                t1.con2, t0.con1, -1);

            MeshHelper.SetAdjacent(tris, t0.adj1, i2, i1, adjacentIndex);
            MeshHelper.SetAdjacent(tris, t1.adj1, i3, i0, triangleIndex);

            Span<VertexMeta> meta = mesh.VertexMetaSpan();
            meta[i0].triangle = meta[i2].triangle = meta[i3].triangle = triangleIndex;
            meta[i1].triangle = adjacentIndex;

            newTris[0] = triangleIndex;
            newTris[1] = adjacentIndex;
            return 2;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanFlipFirstEdge(in Triangle t) => t.adj0 >= 0 && t.con0 < 0;
    }
}
