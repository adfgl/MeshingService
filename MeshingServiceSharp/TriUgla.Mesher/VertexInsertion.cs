using System;
using System.Collections.Generic;
using System.Text;

namespace TriUgla.Mesher
{
    public static class VertexInsertion
    {
        public static int SplitTriangle(this Mesh mesh, int triangleIndex, int vertexIndex)
        {
            List<Triangle> tris = mesh.Triangles;
            List<Circle> crcs = mesh.Circles;

            int t0 = triangleIndex;
            int t1 = tris.Count;
            int t2 = t1 + 1;

            Triangle old = tris[t0];
            int i0 = old.vtx0, i1 = old.vtx1, i2 = old.vtx2, i3 = vertexIndex;

            Span<Vertex> v = Vertices();
            Span<VertexMeta> meta = VertexMeta();

            double x0 = v[i0].x, y0 = v[i0].y;
            double x1 = v[i1].x, y1 = v[i1].y;
            double x2 = v[i2].x, y2 = v[i2].y;
            double x3 = v[i3].x, y3 = v[i3].y;

            // t0: (i0,i1,i3)
            crcs[t0] = Circle.From3Points(x0, y0, x1, y1, x3, y3);
            tris[t0] = new Triangle(i0, i1, i3,
                old.adj0, t1, t2,
                old.con0, -1, -1);

            // t1: (i1,i2,i3)
            crcs.Add(Circle.From3Points(x1, y1, x2, y2, x3, y3));
            tris.Add(new Triangle(i1, i2, i3,
                old.adj1, t2, t0,
                old.con1, -1, -1));

            // t2: (i2,i0,i3)
            crcs.Add(Circle.From3Points(x2, y2, x0, y0, x3, y3));
            tris.Add(new Triangle(i2, i0, i3,
                old.adj2, t0, t1,
                old.con2, -1, -1));

            SetAdjacent(old.adj1, i2, i1, t1);
            SetAdjacent(old.adj2, i0, i2, t2);

            meta[i0].triangle = t0;
            meta[i1].triangle = t0;
            meta[i3].triangle = t0;
            meta[i2].triangle = t1;

            s_new[0] = t0;
            s_new[1] = t1;
            s_new[2] = t2;
            return 3;
        }
    }
}
