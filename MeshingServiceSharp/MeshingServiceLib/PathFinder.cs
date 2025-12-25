namespace MeshingServiceLib
{
    public sealed class PathFinder(Mesh mesh)
    {
        public Mesh Mesh => mesh;

        public int SearchStart { get; set; } = -1;
        public double Eps { get; set; }
        public List<int> Path { get; set; } = new List<int>();

        public int Triangle { get; private set; } = -1;
        public int Edge { get; private set; } = -1;
        public int Vertex { get; private set; } = -1;

        public PathFinder FindContaining(double x, double y)
        {
            Triangle = Edge = Vertex = -1;

            if (Path is null) Path = new List<int>();
            else Path.Clear();

            List<Triangle> tris = Mesh.Triangles;
            int n = tris.Count;
            if (n == 0) return this;

            double eps = Eps;
            double epsSqr = eps * eps;
            int current = SearchStart == -1 ? n - 1 : SearchStart;
            int maxSteps = n * 3;
            int steps = 0;

            List<Vertex> nodes = Mesh.Vertices;
            Vertex vertex = new Vertex(null, x, y, -1);
            while (steps++ < maxSteps)
            {
                Path.Add(current);

                Triangle t = tris[current];
                Vertex v0 = nodes[t.vtx0];
                Vertex v1 = nodes[t.vtx1];
                Vertex v2 = nodes[t.vtx2];

                double cross01 = GeometryHelper.Cross(v0, v1, vertex);
                double cross12 = GeometryHelper.Cross(v1, v2, vertex);
                double cross20 = GeometryHelper.Cross(v2, v0, vertex);

                int bestExit = t.adj0;
                int worstEdge = 0;
                double worstCross = cross01;
                if (cross12 < worstCross)
                {
                    worstCross = cross12;
                    bestExit = t.adj1;
                    worstEdge = 1;
                }

                if (cross20 < worstCross)
                {
                    worstCross = cross20;
                    bestExit = t.adj2;
                    worstEdge = 2;
                }

                if (Math.Abs(worstCross) <= eps)
                {
                    Triangle = current;

                    t.Edge(worstEdge, out int si, out int ei);

                    Vertex start = nodes[si];
                    if (GeometryHelper.LengthSquared(start, vertex) <= epsSqr)
                    {
                        Vertex = si;
                        return this;
                    }

                    Vertex end = nodes[ei];
                    if (GeometryHelper.LengthSquared(end, vertex) <= epsSqr)
                    {
                        Vertex = ei;
                        return this;
                    }

                    if (new Rectangle(start, end).Contains(x, y))
                    {
                        Edge = worstEdge;
                        return this;
                    }
                }

                if (worstCross > 0)
                {
                    Triangle = current;
                    return this;
                }
                current = bestExit;
            }
            return this;
        }
    }
}
