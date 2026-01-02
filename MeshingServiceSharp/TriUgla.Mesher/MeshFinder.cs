using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{

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
            => new (SearchStatus.Found, steps, triangle, edge, -1);

        public static SearchResult Vertex(int steps, int triangle, int vertex)
            => new (SearchResult.Found, steps, triangle, -1, vertex);

        public static SearchResult Triangle(int steps, int triangle)
            => new (SearchResult.Found, steps, triangle, -1, -1);

        public static SearchResult Aborted(int steps = 0) 
            => new (SearchStatus.Aborted, steps, -1, -1, -1);

        public static SearchResult NotFound(int steps = 0) 
            => new (SearchStatus.NotFound, steps, -1, -1, -1);
    }
    public sealed class MeshFinder(Mesh mesh, double eps = 1e-6)
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Triangle> Triangles() => CollectionsMarshal.AsSpan(mesh.Triangles);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Vertex> Vertices() => CollectionsMarshal.AsSpan(mesh.Vertices.Items);

        public double Eps { get; set; } = eps;

        public delegate int EdgeFinder(in Triangle t, int a, int b);

        public SearchResult FindEdgeCore(int start, int end, EdgeFinder finder)
        {
            if (start == end) 
            {
                return SearchResult.Aborted();
            }

            ReadOnlySpan<Triangle> tris = Triangles();
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
                    return SearchResult.Edge(ti, ei);
                }
                steps++;
            }
            while (circler.Next());

            return SearchResult.NotFound(steps);
        }

        public SearchResult FindEdge(int start, int end)
            => FindEdgeCore(start, end, Triangle.IndexOf);

        public SearchResult FindEdgeInvariant(int start, int end)
            => FindEdgeCore(start, end, Triangle.IndexOfInvariant);

        readonly struct TriangleEdge(
            int start, int end, int adjacent)
        {
            public readonly int start = start, end = end, adjacent = adjacent;
        }

        public static bool IsZero(double value, double eps)
        {
            return value <= eps && value >= -eps;
        }

        public static bool AreClose(
            double x0, double y0, 
            double x1, double y1, 
            double eps2)
        {
            double dx = x1 - x0;
            double dy = y1 - y0;
            return dx * dx + dy * dy <= eps2;
        }

        public static bool InRectangle(
            double minX, double minY,
            double maxX, double maxY,
            double x, double y)
        {
            double t;
            if (minX > maxX)
            {
                t = minX;
                minX = maxX;
                maxX = t;
            }

            if (minY > maxY)
            {
                t = minY;
                minY = maxY;
                maxY = t;
            }
            
            return 
                minX < x && x < maxX &&
                minY < y && y < maxY;
        }

        public static int ExitEdgeIndex(
            TriangleEdge[] edges, Span<Vertex> vertices, out double minCross)
        {
            minCross = double.MaxValue;
            int index = -1;
            for (int i = 0; i < edges.Length; i++)
            {
                var edge = edges[i];
                var (xs, ys) = nodes[edge.start];
                var (xe, ye) = nodes[edge.end];

                double cross = GeometryHelper.Cross(xs, ys, ex, ey, x, y);
                if (cross < minCross)
                {
                    minCross = cross;
                    index = i;
                }
            }
            return index;
        }

        public static TringleEdge[] Edges(
            TriangleEdge[] edges, in Triangle t)
        {
            edges[0] = new (t.vtx0, t.vtx1, t.adj0);
            edges[1] = new (t.vtx1, t.vtx2, t.adj1);
            edges[2] = new (t.vtx2, t.vtx0, t.adj2);
            return edges;
        }

        public SearchResult FindContaining(double x, double y, int searchStart = -1, List<int>? path = null)
        {
            Span<Triangle> tris = Triangles();
            Span<Vertex> nodes = Vertices();

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
                
                readonly ref Triangle t = ref tris[triangle];
                int exitEdgeIndex = ExitEdgeIndex(Edges(edges, in t), nodes, out double minCross);
                
                var exitEdge = edges[exitEdgeIndex];
                if (IsZero(minCross, eps))
                {
                    var (xs, ys) = nodes[exitEdge.start];
                    if (AreClose(x, y, xs, ys, epsSqr))
                    {
                        return SearchResult.Vertex(current, exitEdge.start);
                    }

                    var (xe, ye) = nodes[exitEdge.end];
                    if (AreClose(x, y, xe, ye, epsSqr))
                    {
                        return SearchResult.Vertex(current, exitEdge.end);
                    }

                    if (InRectangle(xs, ys, xe, ye, x, y))
                    {
                        return SearchResult.Edge(current, exitEdgeIndex);
                    }
                }

                if (minCross > 0)
                {
                    return SearchResult.Triangle(current);
                }

                if (exitEdge.adjacent == -1)
                {
                    return SearchResult.NotFound(steps);
                }
                current = exitEdge.adjacent;
            }
            return SearchResult.Aborted(steps);
        }
    }
}
