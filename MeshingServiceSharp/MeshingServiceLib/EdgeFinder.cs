namespace MeshingServiceLib
{
    public sealed class EdgeFinder(Mesh mesh)
    {
        public Mesh Mesh => mesh;
        public bool Invariant { get; set; } = false;

        public int Triangle { get; private set; }
        public int Edge { get; private set; }

        public EdgeFinder Find(int start, int end)
        {
            Triangle = Edge = -1;

            List<Triangle> triangles = Mesh.Triangles;

            Vertex startVertex = Mesh.Vertices[start];
            Circler circler = new Circler(triangles, startVertex.Triangle, start);
            do
            {
                int index = circler.Current;
                Triangle t = triangles[index];

                int edge = t.IndexOf(start, end);
                if (edge == -1 && Invariant)
                {
                    edge = t.IndexOf(end, start);
                }

                if (edge != -1)
                {
                    Triangle = index;
                    Edge = edge;
                    break;
                }
            }
            while (circler.Next());
            return this;
        }
    }
}
