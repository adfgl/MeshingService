using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public static class MeshFinder
    {
        public delegate int EdgeFinder(in Triangle t, int a, int b);

        public static SearchResult FindEdgeCore(this Mesh mesh, int start, int end, EdgeFinder finder)
        {
            if (start == end) 
            {
                return SearchResult.Aborted();
            }

            ReadOnlySpan<Triangle> tris = mesh.TrianglesSpan();
            int triangleIndex = mesh.Vertices.Meta[start].triangle;
            Circler circler = new Circler(tris, triangleIndex, start);
            
            int steps = 0;
            do
            {
                int ti = circler.CurrentTriangle;
                ref readonly Triangle t = ref tris[ti];

                int ei = finder(in t, start, end);
                if (ei != -1) 
                {
                    return SearchResult.Edge(steps, ti, ei);
                }
                steps++;
            }
            while (circler.Next());

            return SearchResult.NotFound(steps);
        }

        public static SearchResult FindEdge(int start, int end)
            => FindEdgeCore(start, end, Triangle.IndexOf);

        public static SearchResult FindEdgeInvariant(int start, int end)
            => FindEdgeCore(start, end, Triangle.IndexOfInvariant);

        public static int ExitEdgeIndex(TriangleEdge[] edges, Span<Vertex> vertices, double x, double y, out double minCross)
        {
            minCross = double.MaxValue;
            int index = -1;

            Vertex vtx = new Vertex(x, y, 0);
            for (int i = 0; i < edges.Length; i++)
            {
                TriangleEdge edge = edges[i];
                double cross = GeometryHelper.Cross(in vertices[edge.start], in vertices[edge.end], in vtx);
                if (cross < minCross)
                {
                    minCross = cross;
                    index = i;
                }
            }
            return index;
        }

        public static TriangleEdge[] Edges(
            TriangleEdge[] edges, in Triangle t)
        {
            edges[0] = new (t.vtx0, t.vtx1, t.adj0);
            edges[1] = new (t.vtx1, t.vtx2, t.adj1);
            edges[2] = new (t.vtx2, t.vtx0, t.adj2);
            return edges;
        }

        public static SearchResult FindContaining(this Mesh mesh, double x, double y, int searchStart = -1, List<int>? path = null, double eps = 1e-6)
        {
            Span<Triangle> tris = mesh.TrianglesSpan();
            Span<Vertex> nodes = mesh.VerticesSpan();

            int n = tris.Length;
            if (n == 0) return SearchResult.NotFound();

            double eps = Eps;
            double epsSqr = eps * eps;

            int current = searchStart == -1 ? n - 1 : searchStart;
            int maxSteps = n * 3;
            int steps = 0;

            TriangleEdge[] edges = new TriangleEdge[3];
            while (steps++ < maxSteps)
            {
                path?.Add(current);
                
                ref readonly Triangle t = ref tris[current];
                int exitEdgeIndex = ExitEdgeIndex(Edges(edges, in t), nodes, x, y, out double minCross);
                
                TriangleEdge exitEdge = edges[exitEdgeIndex];
                if (GeometryHelper.IsZero(minCross, eps))
                {
                    var (xs, ys) = nodes[exitEdge.start];
                    if (GeometryHelper.AreClose(x, y, xs, ys, epsSqr))
                    {
                        return SearchResult.Vertex(steps, current, exitEdge.start);
                    }

                    var (xe, ye) = nodes[exitEdge.end];
                    if (GeometryHelper.AreClose(x, y, xe, ye, epsSqr))
                    {
                        return SearchResult.Vertex(steps, current, exitEdge.end);
                    }

                    if (GeometryHelper.InRectangle(xs, ys, xe, ye, x, y))
                    {
                        return SearchResult.Edge(steps, current, exitEdgeIndex);
                    }
                }

                if (minCross > 0)
                {
                    return SearchResult.Triangle(steps, current);
                }

                if (exitEdge.adjacent == -1)
                {
                    return SearchResult.NotFound(steps);
                }
                current = exitEdge.adjacent;
            }
            return SearchResult.Aborted(steps);
        }

        public readonly struct TriangleEdge(
        int start, int end, int adjacent)
        {
            public readonly int start = start, end = end, adjacent = adjacent;
        }
    }

    public enum SearchStatus
    {
        Found, NotFound, Aborted
    }

    public readonly struct SearchResult(
        SearchStatus status,
        int steps,
        int triangle,
        int edge,
        int vertex)
    {
        public readonly SearchStatus status = status;
        public readonly int steps = steps, triangle = triangle, edge = edge, vertex = vertex;

        public static SearchResult Edge(int steps, int triangle, int edge)
            => new(SearchStatus.Found, steps, triangle, edge, -1);

        public static SearchResult Vertex(int steps, int triangle, int vertex)
            => new(SearchStatus.Found, steps, triangle, -1, vertex);

        public static SearchResult Triangle(int steps, int triangle)
            => new(SearchStatus.Found, steps, triangle, -1, -1);

        public static SearchResult Aborted(int steps = 0)
            => new(SearchStatus.Aborted, steps, -1, -1, -1);

        public static SearchResult NotFound(int steps = 0)
            => new(SearchStatus.NotFound, steps, -1, -1, -1);
    }
}
