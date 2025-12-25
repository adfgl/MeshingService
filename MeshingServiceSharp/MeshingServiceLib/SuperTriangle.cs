namespace MeshingServiceLib
{
    public sealed class SuperTriangle : ISuperStructure
    {
        public int SuperVertices => 3;

        public Mesh Build(Polygon<Vertex> polygon, double scale)
        {
            Rectangle bounds = polygon.Bounds;
            double dmax = Math.Max(bounds.maxX - bounds.minX, bounds.maxY - bounds.minY);
            double midx = (bounds.maxX + bounds.minX) * 0.5;
            double midy = (bounds.maxY + bounds.minY) * 0.5;
            double size = Math.Max(scale, 2) * dmax;

            Vertex a = new Vertex("super0", midx - size, midy - size, -1);
            Vertex b = new Vertex("super1", midx + size, midy - size, -1);
            Vertex c = new Vertex("super2", midx, midy + size, -1);
            a.Triangle = b.Triangle = c.Triangle = 0;

            Mesh mesh = new Mesh();
            mesh.Vertices.Add(a);
            mesh.Vertices.Add(b);
            mesh.Vertices.Add(c);

            mesh.Circles.Add(Circle.From3Points(a, b, c));

            mesh.Triangles.Add(new Triangle(0, 1, 2, -1, -1, -1, -1, -1, -1, TriangleState.Keep));

            return mesh;
        }
    }
}
