namespace MeshingServiceLib
{
    public sealed class SuperCircle : ISuperStructure
    {
        public SuperCircle(int vertexCount)
        {
            if (vertexCount < 3) throw new ArgumentOutOfRangeException(nameof(vertexCount));
            SuperVertices = vertexCount;
        }

        public int SuperVertices { get; }

        public Mesh Build(Polygon<Vertex> polygon, double scale)
        {
            Rectangle bounds = polygon.Bounds;
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double radius = Math.Max(scale, 2.0) * dmax;

            int n = SuperVertices;
            Mesh mesh = new Mesh();
            for (int i = 0; i < n; i++)
            {
                double angle = -Math.PI * 0.5 + 2.0 * Math.PI * i / n;
                var v = new Vertex($"super{i}", midx + Math.Cos(angle) * radius, midy + Math.Sin(angle) * radius, -1);
                v.Triangle = 0;
                mesh.Vertices.Add(v);
            }

            int triangleStart = mesh.Triangles.Count;

            for (int i = 0; i < n - 2; i++)
            {
                int v0 = 0;
                int v1 = i + 1;
                int v2 = i + 2;

                int adj0 = i > 0 ? triangleStart + i - 1 : -1;
                int adj1 = i < n - 3 ? triangleStart + i + 1 : -1;
                int adj2 = -1;

                int triIndex = triangleStart + i;

                mesh.Triangles.Add(new Triangle(v0, v1, v2,
                    adj0, adj1, adj2,
                    -1, -1, -1));

                if (mesh.Vertices[v0].Triangle == -1) mesh.Vertices[v0].Triangle = triIndex;
                if (mesh.Vertices[v1].Triangle == -1) mesh.Vertices[v1].Triangle = triIndex;
                if (mesh.Vertices[v2].Triangle == -1) mesh.Vertices[v2].Triangle = triIndex;

                mesh.Circles.Add(Circle.From3Points(mesh.Vertices[v0], mesh.Vertices[v1], mesh.Vertices[v2]));
            }

            return mesh;
        }
    }
}
