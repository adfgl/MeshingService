using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class MeshFinder(Mesh mesh, double eps = 1e-6)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Triangle> Triangles() => CollectionsMarshal.AsSpan(mesh.Triangles);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Vertex> Vertices() => CollectionsMarshal.AsSpan(mesh.Vertices.Items);

        public double Eps { get; set; } = eps;

        public delegate int EdgeFinder(in Triangle t, int a, int b);

        public (int triangleIndex, int edgeIndex) FindEdgeCore(int start, int end, EdgeFinder finder)
        {
            if (start == end) return (-1, -1);

            ReadOnlySpan<Triangle> tris = Triangles();
            int seed = mesh.Vertices.Meta[start].triangle;
            if ((uint)seed >= (uint)tris.Length) return (-1, -1);

            Circler circler = new Circler(tris, seed, start);
            do
            {
                int ti = circler.CurrentTriangle;
                ref readonly Triangle t = ref tris[ti];

                int ei = finder(in t, start, end);
                if (ei != -1) return (ti, ei);
            }
            while (circler.Next());

            return (-1, -1);
        }

        public (int triangleIndex, int edgeIndex) FindEdge(int start, int end)
            => FindEdgeCore(start, end, Triangle.IndexOf);

        public (int triangleIndex, int edgeIndex) FindEdgeInvariant(int start, int end)
            => FindEdgeCore(start, end, Triangle.IndexOfInvariant);

        public (int triangleIndex, int edgeIndex, int vertexIndex) FindContaining(double x, double y, int searchStart = -1, List<int>? path = null)
        {
            Span<Triangle> tris = Triangles();
            int n = tris.Length;
            if (n == 0) return (-1, -1, -1);

            double eps = Eps;
            double epsSqr = eps * eps;

            int current = searchStart == -1 ? n - 1 : searchStart;
            int maxSteps = n * 3;
            int steps = 0;

            Span<Vertex> nodes = Vertices();
            while (steps++ < maxSteps)
            {
                path?.Add(current);

                Triangle t = tris[current];
                int v0 = t.vtx0, v1 = t.vtx1, v2 = t.vtx2;

                Vertex p0 = nodes[v0]; double x0 = p0.x, y0 = p0.y;
                Vertex p1 = nodes[v1]; double x1 = p1.x, y1 = p1.y;
                Vertex p2 = nodes[v2]; double x2 = p2.x, y2 = p2.y;

                int bestExit = t.adj0;
                int worstEdge = 0;
                int start = v0, end = v1;
                double sx = x0, sy = y0, ex = x1, ey = y1;
                double worstCross = GeometryHelper.Cross(x0, y0, x1, y1, x, y);

                double cross12 = GeometryHelper.Cross(x1, y1, x2, y2, x, y);
                if (cross12 < worstCross)
                {
                    worstCross = cross12;
                    bestExit = t.adj1;
                    worstEdge = 1;

                    start = v1; end = v2;
                    sx = x1; sy = y1; ex = x2; ey = y2;
                }

                double cross20 = GeometryHelper.Cross(x2, y2, x0, y0, x, y);
                if (cross20 < worstCross)
                {
                    worstCross = cross20;
                    bestExit = t.adj2;
                    worstEdge = 2;

                    start = v2; end = v0;
                    sx = x2; sy = y2; ex = x0; ey = y0;
                }

                if (worstCross <= eps && worstCross >= -eps)
                {
                    double dx = sx - x, dy = sy - y;
                    if (dx * dx + dy * dy <= epsSqr)
                        return (current, -1, start);

                    dx = ex - x; dy = ey - y;
                    if (dx * dx + dy * dy <= epsSqr)
                        return (current, -1, end);

                    double minX = sx < ex ? sx : ex;
                    double maxX = sx > ex ? sx : ex;
                    double minY = sy < ey ? sy : ey;
                    double maxY = sy > ey ? sy : ey;

                    if (x >= minX && x <= maxX && y >= minY && y <= maxY)
                        return (current, worstEdge, -1);
                }

                if (worstCross > 0)
                {
                    return (current, -1, -1);
                }

                if (bestExit == -1)
                {
                    return (-1, -1, -1);
                }

                current = bestExit;
            }
            return (-1, -1, -1);
        }
    }
}
