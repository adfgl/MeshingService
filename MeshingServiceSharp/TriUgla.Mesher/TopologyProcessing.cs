using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class TopologyProcessing(Mesh mesh)
    {
        readonly int[] s_new = new int[4];

        public void SetAdjacent(int adjIndex, int adjStart, int adjEnd, int value)
        {
            Span<Triangle> tris = CollectionsMarshal.AsSpan(mesh.Triangles);
            if ((uint)adjIndex >= (uint)tris.Length) return;

            ref Triangle t = ref tris[adjIndex];
            int e = t.IndexOf(adjStart, adjEnd);
            if (e == -1)
            {
                throw new InvalidOperationException(
                       $"Triangle {adjIndex} does not contain directed edge ({adjStart} -> {adjEnd}). " +
                       $"Actual edges are ({t.vtx0}->{t.vtx1}), ({t.vtx1}->{t.vtx2}), ({t.vtx2}->{t.vtx0}).");
            }

            if (e == 0) t.adj0 = value;
            else if (e == 1) t.adj1 = value;
            else t.adj2 = value;
        }

        public int Split(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            return edgeIndex == -1 ? SplitTriangle(triangleIndex, vertexIndex) : SplitEdge(triangleIndex, edgeIndex, vertexIndex);
        }


        public int SplitTriangle(int triangleIndex, int vertexIndex)
        {
            List<Triangle> tris = mesh.Triangles;
            List<Circle> crcs = mesh.Circles;

            int t0 = triangleIndex;
            int t1 = tris.Count;
            int t2 = t1 + 1;

            Triangle old = tris[t0];
            int i0 = old.vtx0;
            int i1 = old.vtx1;
            int i2 = old.vtx2;
            int i3 = vertexIndex;

            Span<Vertex> vrts = CollectionsMarshal.AsSpan(mesh.Vertices.Items);
            double x0 = vrts[i0].x, y0 = vrts[i0].y;
            double x1 = vrts[i1].x, y1 = vrts[i1].y;
            double x2 = vrts[i2].x, y2 = vrts[i2].y;
            double x3 = vrts[i3].x, y3 = vrts[i3].y;

            crcs[t0] = Circle.From3Points(x0, y0, x1, y1, x3, y3);
            tris[t0] = new Triangle(i0, i1, i3,
                old.adj0, t1, t2,
                old.con0, -1, -1);

            crcs.Add(Circle.From3Points(x1, y1, x2, y2, x3, y3));
            tris.Add(new Triangle(i1, i2, i3,
                old.adj1, t2, t0,
                old.con1, -1, -1));

            crcs.Add(Circle.From3Points(x2, y2, x0, y0, x3, y3));
            tris.Add(new Triangle(i2, i0, i3,
               old.adj2, t0, t1,
               old.con2, -1, -1));

            SetAdjacent(old.adj1, i2, i1, t1);
            SetAdjacent(old.adj2, i0, i2, t2);

            Span<VertexMeta> meta = CollectionsMarshal.AsSpan(mesh.Vertices.Meta);
            ref VertexMeta vm0 = ref meta[i0];
            ref VertexMeta vm1 = ref meta[i1];
            ref VertexMeta vm2 = ref meta[i2];
            ref VertexMeta vm3 = ref meta[i3];

            vm0.triangle = vm1.triangle = vm3.triangle = t0;
            vm2.triangle = t1;

            s_new[0] = t0;
            s_new[1] = t1;
            s_new[2] = t2;
            return 3;
        }

        public int SplitEdge(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            Triangle old0 = mesh.Triangles[triangleIndex].Orient(edgeIndex);

            if (old0.adj0 == -1) 
                return SplitEdgeNoAdjacent(in old0, triangleIndex, vertexIndex);
            return SplitEdgeHasAdjacent(in old0, triangleIndex, vertexIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SplitConstraintEdge(int conIndex, int newVertex, int leftTriangle, int rightTriangle)
        {
            if (conIndex == -1) return;

            List<Edge> edgeItems = mesh.Edges.Items;
            List<EdgeMeta> edgeMeta = mesh.Edges.Meta;

            // Overwrite old segment
            Edge e = edgeItems[conIndex];
            EdgeMeta em = edgeMeta[conIndex];

            edgeItems[conIndex] = new Edge(e.start, newVertex);
            edgeMeta[conIndex] = new EdgeMeta(leftTriangle, em.id);

            // Append new segment
            edgeItems.Add(new Edge(newVertex, e.end));
            edgeMeta.Add(new EdgeMeta(rightTriangle, em.id));
        }

        int SplitEdgeHasAdjacent(in Triangle old0, int triangleIndex, int vertexIndex)
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

            List<Triangle> tris = mesh.Triangles;
            List<Circle> crcs = mesh.Circles;

            int t0 = triangleIndex;
            int t1 = old0.adj0;
            int t2 = tris.Count;
            int t3 = t2 + 1;

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;
            int i3 = vertexIndex;

            int con = old0.con0;

            Triangle old1 = tris[t1];
            old1 = old1.Orient(old1.IndexOf(i1, i0));

            int i4 = old1.vtx2;

            // 1) 2-0-3
            // 2) 0-4-3
            // 3) 4-1-3
            // 4) 1-2-3

            Span<Vertex> vrts = CollectionsMarshal.AsSpan(mesh.Vertices.Items);
            double x0 = vrts[i0].x, y0 = vrts[i0].y;
            double x1 = vrts[i1].x, y1 = vrts[i1].y;
            double x2 = vrts[i2].x, y2 = vrts[i2].y;
            double x3 = vrts[i3].x, y3 = vrts[i3].y;
            double x4 = vrts[i4].x, y4 = vrts[i4].y;

            crcs[t0] = Circle.From3Points(x2, y2, x0, y0, x3, y3);
            tris[t0] = new Triangle(i2, i0, i3,
                old0.adj2, t1, t3,
                old0.con2, con, -1);

            crcs[t1] = Circle.From3Points(x0, y0, x4, y4, x3, y3);
            tris[t1] = new Triangle(i0, i4, i3,
                old1.adj1, t2, t0,
                old1.con1, -1, con);

            crcs.Add(Circle.From3Points(x4, y4, x1, y1, x3, y3));
            tris.Add(new Triangle(i4, i1, i3,
                old1.adj2, t3, t1,
                old1.con2, con, -1));

            crcs.Add(Circle.From3Points(x1, y1, x2, y2, x3, y3));
            tris.Add(new Triangle(i1, i2, i3,
                old0.adj1, t0, t2,
                old0.con1, -1, con));

            SetAdjacent(old0.adj1, i2, i1, t3);
            SetAdjacent(old1.adj2, i1, i3, t2);

            Span<VertexMeta> meta = CollectionsMarshal.AsSpan(mesh.Vertices.Meta);
            ref VertexMeta vm0 = ref meta[i0];
            ref VertexMeta vm1 = ref meta[i1];
            ref VertexMeta vm2 = ref meta[i2];
            ref VertexMeta vm3 = ref meta[i3];
            ref VertexMeta vm4 = ref meta[i4];

            vm0.triangle = vm2.triangle = t0;
            vm1.triangle = t3;
            vm3.triangle = t1;

            SplitConstraintEdge(old0.con0, i3, t0, t3);
            SplitConstraintEdge(old1.con0, i3, t2, t1);

            s_new[0] = t0;
            s_new[1] = t1;
            s_new[2] = t2;
            s_new[3] = t3;
            return 4;
        }

        int SplitEdgeNoAdjacent(in Triangle old0, int triangleIndex, int vertexIndex)
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

            List<Triangle> tris = mesh.Triangles;
            List<Circle> crcs = mesh.Circles;

            int t0 = triangleIndex;
            int t1 = tris.Count;

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;
            int i3 = vertexIndex;

            int con = old0.con0;

            Span<Vertex> vrts = CollectionsMarshal.AsSpan(mesh.Vertices.Items);
            double x0 = vrts[i0].x, y0 = vrts[i0].y;
            double x1 = vrts[i1].x, y1 = vrts[i1].y;
            double x2 = vrts[i2].x, y2 = vrts[i2].y;
            double x3 = vrts[i3].x, y3 = vrts[i3].y;

            crcs[t0] = Circle.From3Points(x2, y2, x0, y0, x3, y3);
            tris[t0] = new Triangle(i2, i0, i3,
                old0.adj2, -1, t1,
                old0.con2, con, -1);

            crcs.Add(Circle.From3Points(x1, y1, x2, y2, x3, y3));
            tris.Add(new Triangle(i1, i2, i3,
                old0.adj1, t0, -1,
                old0.con1, -1, con));

            SetAdjacent(old0.adj1, i2, i1, t1);

            Span<VertexMeta> meta = CollectionsMarshal.AsSpan(mesh.Vertices.Meta);
            ref VertexMeta vm0 = ref meta[i0];
            ref VertexMeta vm1 = ref meta[i1];
            ref VertexMeta vm2 = ref meta[i2];
            ref VertexMeta vm3 = ref meta[i3];

            vm0.triangle = vm2.triangle = vm3.triangle = t0;
            vm1.triangle = t1;

            SplitConstraintEdge(con, i3, t0, t1);

            s_new[0] = t0;
            s_new[1] = t1;
            return 2;
        }
    }
}
