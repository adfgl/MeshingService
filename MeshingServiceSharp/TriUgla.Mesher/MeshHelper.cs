using System.Runtime.CompilerServices;

namespace TriUgla.Mesher
{
    public static class MeshHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emit(Span<int> dst, int a, int b) { dst[0] = a; dst[1] = b; return 2; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emit(Span<int> dst, int a, int b, int c) { dst[0] = a; dst[1] = b; dst[2] = c; return 3; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Emit(Span<int> dst, int a, int b, int c, int d) { dst[0] = a; dst[1] = b; dst[2] = c; dst[3] = d; return 4; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteTriangle(
            this Mesh mesh,
            int ti,
            int a, int b, int c,
            int adj0, int adj1, int adj2,
            int con0, int con1, int con2)
        {
            Span<Vertex> v = mesh.VerticesSpan();
            mesh.Circles[ti] = Circle.From3Points(in v[a], in v[b], in v[c]);
            mesh.Triangles[ti] = new Triangle(a, b, c, adj0, adj1, adj2, con0, con1, con2);

            Span<VertexMeta> meta = mesh.VertexMetaSpan();
            meta[a].triangle = meta[b].triangle = meta[c].triangle = ti;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AddTriangle(
            this Mesh mesh,
            int a, int b, int c,
            int adj0, int adj1, int adj2,
            int con0, int con1, int con2)
        {
            Span<Vertex> v = mesh.VerticesSpan();
            int ti = mesh.Triangles.Count;
            mesh.Circles.Add(Circle.From3Points(in v[a], in v[b], in v[c]));
            mesh.Triangles.Add(new Triangle(a, b, c, adj0, adj1, adj2, con0, con1, con2));

            Span<VertexMeta> meta = mesh.VertexMetaSpan();
            meta[a].triangle = meta[b].triangle = meta[c].triangle = ti;
            return ti;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAdjacent(Span<Triangle> tris, int adjIndex, int adjStart, int adjEnd, int value)
        {
            if (adjIndex < 0) return;

            ref Triangle t = ref tris[adjIndex];
            int e = Triangle.IndexOf(in t, adjStart, adjEnd);
            if (e < 0)
            {
                throw new InvalidOperationException(
                    $"Triangle {adjIndex} does not contain directed edge ({adjStart}->{adjEnd}). " +
                    $"Edges: ({t.vtx0}->{t.vtx1}), ({t.vtx1}->{t.vtx2}), ({t.vtx2}->{t.vtx0}).");
            }

            if (e == 0) t.adj0 = value;
            else if (e == 1) t.adj1 = value;
            else t.adj2 = value;
        }

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
            Triangle t0 = tris[triangleIndex];
            if (edgeIndex != 0) t0 = t0.Orient(edgeIndex);

            if (!CanFlipFirstEdge(in t0)) return -1;

            int adjacentIndex = t0.adj0;
            Triangle t1 = tris[adjacentIndex];
            int e1 = Triangle.IndexOf(in t1, t0.vtx1, t0.vtx0);
            if (e1 != 0) t1 = t1.Orient(e1);

            int v0 = t0.vtx0;
            int v1 = t0.vtx1;
            int v2 = t0.vtx2;
            int v3 = t1.vtx2;

            mesh.WriteTriangle(triangleIndex,
                v0, v3, v2,
                t1.adj1, adjacentIndex, t0.adj2,
                t1.con1, -1, t0.con2);

            mesh.WriteTriangle(adjacentIndex,
                v3, v1, v2,
                t1.adj2, t0.adj1, triangleIndex,
                t1.con2, t0.con1, -1);

            SetAdjacent(tris, t0.adj1, v2, v1, adjacentIndex);
            SetAdjacent(tris, t1.adj1, v3, v0, triangleIndex);
            return Emit(newTris, triangleIndex, adjacentIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CanFlipFirstEdge(in Triangle t) => t.adj0 >= 0 && t.con0 < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Split(this Mesh mesh, Span<int> newTriangles, int triangleIndex, int edgeIndex, int vertexIndex)
               => edgeIndex < 0 ? SplitTriangle(mesh, newTriangles, triangleIndex, vertexIndex)
                       : SplitEdge(mesh, newTriangles, triangleIndex, edgeIndex, vertexIndex);

        public static int SplitTriangle(this Mesh mesh, Span<int> newTriangles, int triangleIndex, int vertexIndex)
        {
            int t0 = triangleIndex;
            int t1 = mesh.Triangles.Count;
            int t2 = t1 + 1;

            Triangle old = mesh.Triangles[t0];
            int i0 = old.vtx0;
            int i1 = old.vtx1;
            int i2 = old.vtx2;
            int i3 = vertexIndex;

            mesh.WriteTriangle(t0, i0, i1, i3,
                old.adj0, t1, t2,
                old.con0, -1, -1);

            mesh.AddTriangle(i1, i2, i3,
                old.adj1, t2, t0,
                old.con1, -1, -1);

            mesh.AddTriangle(i2, i0, i3,
                old.adj2, t0, t1,
                old.con2, -1, -1);

            Span<Triangle> t = mesh.TrianglesSpan();
            SetAdjacent(t, old.adj1, i2, i1, t1);
            SetAdjacent(t, old.adj2, i0, i2, t2);
            return Emit(newTriangles, t0, t1, t2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SplitEdge(this Mesh mesh, Span<int> newTriangles, int triangleIndex, int edgeIndex, int vertexIndex)
        {
            Triangle old0 = mesh.Triangles[triangleIndex].Orient(edgeIndex);
            return old0.adj0 < 0
                ? SplitFirstEdgeNoAdjacent(mesh, newTriangles, in old0, triangleIndex, vertexIndex)
                : SplitFirstEdge(mesh, newTriangles, in old0, triangleIndex, vertexIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SplitConstraintEdge(List<Edge> edges, int conIndex, int newV, int triForStartToNew, int triForNewToEnd)
        {
            if (conIndex < 0) return;
            Edge edge = edges[conIndex];
            edges[conIndex] = new Edge(edge.start, newV, triForStartToNew, edge.id);
            edges.Add(new Edge(newV, edge.end, triForNewToEnd, edge.id));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SplitFirstEdge(this Mesh mesh, Span<int> newTriangles, in Triangle old0, int triangleIndex, int vertexIndex)
        {
            /* 
              *        v2                   v2
              *        /\                  /|\
              *       /  \                / | \
              *      /    \              /  |  \
              *     /  t0  \            /t0 | t3\
              *    /   →    \          /    |    \
              * v0+----------+v1    v0+-----v3----+v1
              *    \   ←    /          \    |    /
              *     \  t1  /            \t1 | t2/
              *      \    /              \  |  /
              *       \  /                \ | /
              *        \/                  \|/
              *        v4                   v4
              */

            int t0 = triangleIndex;
            int t1 = old0.adj0;
            int t2 = mesh.Triangles.Count;
            int t3 = t2 + 1;

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;
            int i3 = vertexIndex;
            int con = old0.con0;

            Triangle old1 = mesh.Triangles[t1];
            int edge = Triangle.IndexOf(in old1, i1, i0);
            if (edge != 0) old1 = old1.Orient(edge);
            int i4 = old1.vtx2;

            mesh.WriteTriangle(t0, i2, i0, i3,
                old0.adj2, t1, t3,
                old0.con2, con, -1);

            mesh.WriteTriangle(t1, i0, i4, i3,
                old1.adj1, t2, t0,
                old1.con1, -1, con);

            mesh.AddTriangle(i4, i1, i3,
                old1.adj2, t3, t1,
                old1.con2, con, -1);

            mesh.AddTriangle(i1, i2, i3,
               old0.adj1, t0, t2,
               old0.con1, -1, con);

            Span<Triangle> tspan = mesh.TrianglesSpan();
            SetAdjacent(tspan, old0.adj1, i2, i1, t3);
            SetAdjacent(tspan, old1.adj2, i1, i3, t2);

            SplitConstraintEdge(mesh.Edges, old0.con0, i3, t0, t3);
            SplitConstraintEdge(mesh.Edges, old1.con0, i3, t2, t1);
            return Emit(newTriangles, t0, t1, t2, t3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SplitFirstEdgeNoAdjacent(this Mesh mesh, Span<int> newTriangles, in Triangle old0, int triangleIndex, int vertexIndex)
        {
            /*  
              *        v2                   v2
              *        /\                  /|\
              *       /  \                / | \
              *      /    \              /  |  \
              *     /  t0  \            / t0|t1 \
              *    /   →    \          /    |    \
              * v0+----------+v1    v0+-----+-----+v1
              *                            v3
              */

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;
            int i3 = vertexIndex;
            int con = old0.con0;

            int t0 = triangleIndex;
            int t1 = mesh.AddTriangle(i1, i2, i3,
                old0.adj1, t0, -1,
                old0.con1, -1, con);

            mesh.WriteTriangle(t0, i2, i0, i3,
                old0.adj2, -1, t1,
                old0.con2, con, -1);

            Span<Triangle> tspan = mesh.TrianglesSpan();
            SetAdjacent(tspan, old0.adj1, i2, i1, t1);

            SplitConstraintEdge(mesh.Edges, con, i3, t0, t1);
            return Emit(newTriangles, t0, t1);
        }
    }
}
