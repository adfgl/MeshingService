namespace MeshingServiceLib
{
    public class TriangleFlipper(Mesh mesh)
    {
        readonly int[] _newTriangles = new int[4];
        public Mesh Mesh => mesh;
        public int[] NewTriangles => _newTriangles;
        

        public void SetAdjacent(int adjIndex, int adjStart, int adjEnd, int value)
        {
            if (adjIndex == -1) return;

            Triangle t = Mesh.Triangles[adjIndex];
            switch (t.IndexOf(adjStart, adjEnd))
            {
                case 0:
                    t.adj0 = value;
                    break;

                case 1:
                    t.adj1 = value;
                    break;

                case 2:
                    t.adj2 = value;
                    break;

                default:
                    throw new Exception();
            }

            Mesh.Triangles[adjIndex] = t;
        }
        

        public int Flip(int t0, int e0, bool forceFlip)
        {
            List<Triangle> triangles = Mesh.Triangles;
            List<Vertex> vertices = Mesh.Vertices;
            List<Circle> circles = Mesh.Circles;

            /*  # ROTATE EDGE COUNTER-CLOCKWISE #
             *        v2                   v2
             *        /\                  /|\
             *       /  \                / | \
             *      /    \              /  |  \
             *     /  t0  \            /   |   \
             *    /   →    \          /    |    \
             * v0+----------+v1    v0+ t0 ↓|↑ t1 +v1
             *    \   ←    /          \    |    /
             *     \  t1  /            \   |   /
             *      \    /              \  |  /
             *       \  /                \ | /
             *        \/                  \|/
             *        v3                   v3
             */


            Triangle old0 = triangles[t0].Orient(e0);
            bool constraint = old0.con0;

            if (old0.adj0 == -1 // can't flip if adjacent does not exist
                || 
                (constraint // can't flip constrained...
                && 
                !forceFlip)) // ..unless forced
            {
                return 0;
            }

            int i0 = old0.vtx0;
            int i1 = old0.vtx1;
            int i2 = old0.vtx2;

            int t1 = old0.adj0;
            Triangle old1 = triangles[t1];
            old1 = old1.Orient(i1, i0);

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
                 old1.con1, constraint, old0.con2, state);

            circles[t1] = Circle.From3Points(v3, v1, v2);
            triangles[t1] = new Triangle(i3, i1, i2,
                old1.adj2, old0.adj1, t0,
                old1.con2, old0.con1, constraint, state);

            // as a result two triangles have 'lost' their neigbours
            // namely: t1(v0-v3) & t0 (v1-v2)
            SetAdjacent(old0.adj1, i3, i0, t0);
            SetAdjacent(old1.adj1, i1, i2, t1);

            v0.Triangle = v2.Triangle = v3.Triangle = t0;
            v1.Triangle = t1;

            _newTriangles[0] = t0;
            _newTriangles[1] = t1;
            return 2;
        }
    }
}
