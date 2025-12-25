namespace MeshingServiceLib
{
    public sealed class TriangleSplitter(Mesh mesh) : TriangleProcessor(mesh)
    {
        public int Split(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            if (edgeIndex == -1)
            {
                return SplitEdge(triangleIndex, edgeIndex, vertexIndex);
            }
            return SplitTriangle(triangleIndex, vertexIndex);
        }

        public int SplitTriangle(int t0, int vertexIndex)
        {
            List<Triangle> triangles = Mesh.Triangles;
            List<Vertex> vertices = Mesh.Vertices;
            List<Circle> circles = Mesh.Circles;

            Triangle old = triangles[t0];

            int i0 = old.vtx0;
            int i1 = old.vtx1;
            int i2 = old.vtx2;
            int i3 = vertexIndex;

            Vertex v0 = vertices[i0];
            Vertex v1 = vertices[i1];
            Vertex v2 = vertices[i2];
            Vertex v3 = vertices[i3];

            // 1) 0-1-3
            // 2) 1-2-3
            // 3) 2-0-3
            int t1 = triangles.Count;
            int t2 = t1 + 1;

            circles[t0] = Circle.From3Points(v0, v1, v3);
            triangles[t0] = new Triangle(i0, i1, i3,
                old.adj0, t1, t2,
                old.con0, -1, -1,
                old.state);

            circles.Add(Circle.From3Points(v1, v2, v3));
            triangles.Add(new Triangle(i1, i2, i3,
                old.adj1, t2, t0,
                old.con1, -1, -1,
                old.state));

            circles.Add(Circle.From3Points(v2, v0, v3));
            triangles.Add(new Triangle(i2, i0, i3,
               old.adj2, t0, t1,
               old.con2, -1, -1,
               old.state));

            SetAdjacent(old.adj1, i2, i1, t1);
            SetAdjacent(old.adj2, i0, i2, t2);

            v0.Triangle = v1.Triangle = v3.Triangle = t0;
            v2.Triangle = t1;

            NewTriangles[0] = t0;
            NewTriangles[1] = t1;
            NewTriangles[2] = t2;
            return 3;
        }

        public int SplitEdge(int t0, int edgeIndex, int vertexIndex)
        {
            List<Triangle> triangles = Mesh.Triangles;
            List<Vertex> vertices = Mesh.Vertices;
            List<Circle> circles = Mesh.Circles;

            Triangle old0 = triangles[t0].Orient(edgeIndex);
            int con = old0.con0;

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;
            int i3 = vertexIndex;

            Vertex v0 = vertices[i0];
            Vertex v1 = vertices[i1];
            Vertex v2 = vertices[i2];
            Vertex v3 = vertices[i3];

            int adj = old0.adj0;
            if (adj == -1)
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

                int t1 = triangles.Count;

                // 1) 2-0-3
                // 2) 1-2-3
                circles[t0] = Circle.From3Points(v2, v0, v3);
                triangles[t0] = new Triangle(i2, i0, i3,
                    old0.adj2, -1, t1,
                    old0.con2, con, -1,
                    old0.state);

                circles.Add(Circle.From3Points(v2, v1, v3));
                triangles.Add(new Triangle(i2, i1, i3,
                    old0.adj1, t0, -1,
                    old0.con1, -1, con,
                    old0.state));

                SetAdjacent(old0.adj1, i2, i1, t1);

                v0.Triangle = v2.Triangle = v3.Triangle = t0;
                v1.Triangle = t1;

                NewTriangles[0] = t0;
                NewTriangles[1] = t1;

                if (con != -1)
                {
                    Split(con, i3, out TriangleEdge a, out TriangleEdge b);
                    a.triangle = t0;
                    b.triangle = t1;

                    Mesh.Edges[con] = a;
                    Mesh.Edges.Add(b);
                }
                return 2;
            }
            else
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

                int t1 = adj;
                Triangle old1 = triangles[t1].Orient(i1, i0);

                int t2 = triangles.Count;
                int t3 = t2 + 1;

                int i4 = old1.vtx2;
                Vertex v4 = vertices[i4];

                // 1) 2-0-3
                // 2) 0-4-3
                // 3) 4-1-3
                // 4) 1-2-3
                circles[t0] = Circle.From3Points(v2, v0, v3);
                triangles[t0] = new Triangle(i2, i0, i3,
                    old0.adj2, t1, t3,
                    old0.con2, con, -1,
                    old0.state);

                circles[t1] = Circle.From3Points(v0, v4, v3);
                triangles[t1] = new Triangle(i0, i4, i3,
                    old1.adj1, t2, t0,
                    old1.con1, -1, con,
                    old1.state);

                circles.Add(Circle.From3Points(v4, v1, v3));
                triangles.Add(new Triangle(i4, i1, i3,
                    old1.adj2, t3, t1,
                    old1.con2, con, -1,
                    old1.state));

                circles.Add(Circle.From3Points(v1, v2, v3));
                triangles.Add(new Triangle(i1, i2, i3,
                    old0.adj1, t0, t2,
                    old0.con1, -1, con,
                    old0.state));

                SetAdjacent(old0.adj1, i2, i1, t3);
                SetAdjacent(old1.adj2, i1, i3, t2);

                v0.Triangle = v2.Triangle = t0;
                v1.Triangle = t3;
                v3.Triangle = t1;

                NewTriangles[0] = t0;
                NewTriangles[1] = t1;
                NewTriangles[2] = t2;
                NewTriangles[3] = t3;

                con = old0.con0;
                if (con != -1)
                {
                    Split(con, i3, out TriangleEdge a, out TriangleEdge b);
                    a.triangle = t0;
                    b.triangle = t3;

                    Mesh.Edges[con] = a;
                    Mesh.Edges.Add(b);
                }

                con = old1.con0;
                if (con != -1)
                {
                    Split(con, i3, out TriangleEdge a, out TriangleEdge b);
                    a.triangle = t2;
                    b.triangle = t1;

                    Mesh.Edges[con] = a;
                    Mesh.Edges.Add(b);
                }
                return 4;
            }
       
        }
    }
}
