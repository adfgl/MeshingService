namespace MeshingServiceLib
{
    public class TriangleFlipper(Mesh mesh) : TriangleProcessor(mesh)
    {
        public bool CanFlip(int t0, int e0, out bool should)
        {
            should = false;

            List<Vertex> nodes = Mesh.Vertices;
            List<Triangle> tris = Mesh.Triangles;

            Triangle tri0 = tris[t0].Orient(e0);

            int t1 = tri0.adj0;
            if (t1 == -1 || tri0.con0 != -1)
            {
                return false;
            }

            Vertex v0 = nodes[tri0.vtx0];
            Vertex v1 = nodes[tri0.vtx1];
            Vertex v2 = nodes[tri0.vtx2];

            Triangle tri1 = tris[t1];
            int e1 = tri1.IndexOf(tri0.vtx1, tri0.vtx0);

            int i3 = e1 switch
            {
                0 => tri1.vtx2,
                1 => tri1.vtx0,
                2 => tri1.vtx1,
                _ => throw new Exception(),
            };

            Vertex v3 = nodes[i3];
            should = Mesh.Circles[t0].Contains(v3.X, v3.Y);
            return GeometryHelper.IsConvex(v1, v2, v0, v3);
        }

        public int Flip(int t0, int e0, bool forceFlip)
        {
            List<Triangle> triangles = Mesh.Triangles;
            List<Vertex> vertices = Mesh.Vertices;
            List<Circle> circles = Mesh.Circles;

            /*  
             *        v2                   v2
             *        /\                  /|\
             *       /  \                / | \
             *      /    \              /  |  \
             *     /  t0  \            /   |   \
             *    /   →    \          /    |    \
             * v0+----------+v1    v0+ t0 ↑|↓ t1 +v1
             *    \   ←    /          \    |    /
             *     \  t1  /            \   |   /
             *      \    /              \  |  /
             *       \  /                \ | /
             *        \/                  \|/
             *        v3                   v3
             */


            Triangle old0 = triangles[t0].Orient(e0);
            int con = old0.con0;
            if (old0.adj0 == -1 // can't flip if adjacent does not exist
                || 
                (con != -1 // can't flip constrained...
                && 
                !forceFlip)) // ..unless forced
            {
                return 0;
            }

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;

            int t1 = old0.adj0;
            Triangle old1 = triangles[t1].Orient(i1, i0);

            int i3 = old1.vtx2;

            Vertex v0 = vertices[i0];
            Vertex v1 = vertices[i1];
            Vertex v2 = vertices[i2];
            Vertex v3 = vertices[i3];

            TriangleState state = (old0.state == old1.state) ? old0.state : TriangleState.Ambiguous;

            // the first diagonal MUST be opposite to v2 to avoid degeneracy
            // 1) 0-3-2
            // 2) 3-1-2
            circles[t0] = Circle.From3Points(v0, v3, v2);
            triangles[t0] = new Triangle(i0, i3, i2,
                 old1.adj1, t1, old0.adj2,
                 old1.con1, con, old0.con2, state);

            circles[t1] = Circle.From3Points(v3, v1, v2);
            triangles[t1] = new Triangle(i3, i1, i2,
                old1.adj2, old0.adj1, t0,
                old1.con2, old0.con1, con, state);

            // as a result two triangles have 'lost' their neigbours
            // namely: t1(v0-v3) & t0 (v1-v2)
            SetAdjacent(old0.adj1, i3, i0, t0);
            SetAdjacent(old1.adj1, i1, i2, t1);

            v0.Triangle = v2.Triangle = v3.Triangle = t0;
            v1.Triangle = t1;

            NewTriangles[0] = t0;
            NewTriangles[1] = t1;

            if (con != -1)
            {
                TriangleEdge edge = Mesh.Edges[con];
                Mesh.Edges[con] = new TriangleEdge(edge.id, i3, i2, t0);
            }

            con = old1.con0;
            if (con != -1)
            {
                TriangleEdge edge = Mesh.Edges[con];
                Mesh.Edges[con] = new TriangleEdge(edge.id, i2, i3, t1);
            }
            return 2;
        }
    }
}
