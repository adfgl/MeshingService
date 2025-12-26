using System;
using System.Collections.Generic;
using System.Text;

namespace MeshingServiceLib
{
    public static class MeshCompactor
    {
        public static Mesh BuildCleanMesh(Mesh src, Shape shape, double eps, int superCount = 3)
        {
            var oldV = src.Vertices;
            var oldT = src.Triangles;
            var oldE = src.Edges;

            int tCount = oldT.Count;
            if (tCount == 0) return new Mesh();

            // ── 1. Keep mask ─────────────────────────────────────────────

            bool[] keep = new bool[tCount];
            for (int ti = 0; ti < tCount; ti++)
            {
                Triangle t = oldT[ti];
                if (t.vtx0 < superCount || t.vtx1 < superCount || t.vtx2 < superCount)
                    continue;

                Vertex a = oldV[t.vtx0];
                Vertex b = oldV[t.vtx1];
                Vertex c = oldV[t.vtx2];

                double cx = (a.X + b.X + c.X) / 3.0;
                double cy = (a.Y + b.Y + c.Y) / 3.0;

                if (shape.Contains(cx, cy, eps))
                    keep[ti] = true;
            }

            // ── 2. Triangle remap ─────────────────────────────────────────

            int[] triMap = new int[tCount];
            Array.Fill(triMap, -1);

            int newTriCount = 0;
            for (int i = 0; i < tCount; i++)
                if (keep[i])
                    triMap[i] = newTriCount++;

            if (newTriCount == 0)
                return new Mesh();

            // ── 3. Used vertices ──────────────────────────────────────────

            bool[] usedV = new bool[oldV.Count];
            for (int ti = 0; ti < tCount; ti++)
                if (keep[ti])
                {
                    Triangle t = oldT[ti];
                    usedV[t.vtx0] = true;
                    usedV[t.vtx1] = true;
                    usedV[t.vtx2] = true;
                }

            // ── 4. Vertex remap (this drops super vertices and renumbers) ─

            int[] vMap = new int[oldV.Count];
            Array.Fill(vMap, -1);

            var newVerts = new List<Vertex>();
            for (int i = superCount; i < oldV.Count; i++)
            {
                if (!usedV[i]) continue;

                vMap[i] = newVerts.Count;
                Vertex v = oldV[i];
                v.Triangle = -1;
                newVerts.Add(v);
            }

            // ── 5. Build new triangles + constraints ──────────────────────

            var newTris = new List<Triangle>(newTriCount);
            var newEdges = new List<TriangleEdge>();

            for (int oldTi = 0; oldTi < tCount; oldTi++)
            {
                if (!keep[oldTi]) continue;

                Triangle t = oldT[oldTi];

                int a = vMap[t.vtx0];
                int b = vMap[t.vtx1];
                int c = vMap[t.vtx2];

                int adj0 = t.adj0 >= 0 && keep[t.adj0] ? triMap[t.adj0] : -1;
                int adj1 = t.adj1 >= 0 && keep[t.adj1] ? triMap[t.adj1] : -1;
                int adj2 = t.adj2 >= 0 && keep[t.adj2] ? triMap[t.adj2] : -1;

                int newTi = newTris.Count;

                int con0 = CopyConstraint(t.con0, a, b, newTi, oldE, newEdges);
                int con1 = CopyConstraint(t.con1, b, c, newTi, oldE, newEdges);
                int con2 = CopyConstraint(t.con2, c, a, newTi, oldE, newEdges);

                newTris.Add(new Triangle(a, b, c, adj0, adj1, adj2, con0, con1, con2));

                FixVertexTriangle(newVerts, a, newTi);
                FixVertexTriangle(newVerts, b, newTi);
                FixVertexTriangle(newVerts, c, newTi);
            }

            Mesh mesh = new Mesh();
            mesh.Vertices.AddRange(newVerts);
            mesh.Triangles.AddRange(newTris);
            mesh.Edges.AddRange(newEdges);
            return mesh;
        }

        static int CopyConstraint(
            int oldIdx, int a, int b, int tri,
            List<TriangleEdge> oldEdges, List<TriangleEdge> newEdges)
        {
            if (oldIdx < 0 || oldIdx >= oldEdges.Count)
                return -1;

            string? id = oldEdges[oldIdx].id;
            int ni = newEdges.Count;
            newEdges.Add(new TriangleEdge(id, a, b, tri));
            return ni;
        }

        static void FixVertexTriangle(List<Vertex> v, int vi, int ti)
        {
            if (v[vi].Triangle >= 0) return;
            Vertex vv = v[vi];
            vv.Triangle = ti;
            v[vi] = vv;
        }
    }
}
