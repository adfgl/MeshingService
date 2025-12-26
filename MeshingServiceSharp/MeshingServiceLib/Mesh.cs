        using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace MeshingServiceLib
{
    public sealed class Mesh
    {
        public List<Vertex> Vertices { get; } = new();
        public List<Circle> Circles { get; } = new();
        public List<Triangle> Triangles { get; } = new();
        public List<TriangleEdge> Edges { get; } = new();

        readonly int[] _newTriangles = new int[4];
        internal int[] NewTriangles => _newTriangles;

        public static (int triangleIndex, int edgeIndex) FindEdge(Mesh mesh, int start, int end, bool invariant)
        {
            List<Triangle> triangles = mesh.Triangles;

            int triangleIndex = -1, edgeIndex = -1;

            Vertex startVertex = mesh.Vertices[start];
            Circler circler = new Circler(triangles, startVertex.Triangle, start);
            do
            {
                int index = circler.Current;
                Triangle t = triangles[index];

                edgeIndex = invariant ? t.IndexOfInvariant(start, end) : t.IndexOf(start, end);
                if (edgeIndex != -1)
                {
                    triangleIndex = index;
                    break;
                }
            }
            while (circler.Next());
            return (triangleIndex, edgeIndex);
        }

        public static (int triangleIndex, int edgeIndex, int vertexIndex) FindContaining(Mesh mesh, double x, double y, double eps, int searchStart = -1, List<int>? path = null)
        {
            List<Triangle> tris = mesh.Triangles;
            int n = tris.Count;
            if (n == 0) return (-1, -1, -1);

            double epsSqr = eps * eps;
            int current = searchStart == -1 ? n - 1 : searchStart;
            int maxSteps = n * 3;
            int steps = 0;

            List<Vertex> nodes = mesh.Vertices;
            while (steps++ < maxSteps)
            {
                path?.Add(current);

                Triangle t = tris[current];
                Vertex v0 = nodes[t.vtx0];
                Vertex v1 = nodes[t.vtx1];
                Vertex v2 = nodes[t.vtx2];

                double cross01 = GeometryHelper.Cross(v0, v1, x, y);
                double cross12 = GeometryHelper.Cross(v1, v2, x, y);
                double cross20 = GeometryHelper.Cross(v2, v0, x, y);

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
                    t.Edge(worstEdge, out int si, out int ei);

                    Vertex start = nodes[si];
                    if (GeometryHelper.LengthSquared(start, x, y) <= epsSqr)
                    {
                        return (current, -1, si);
                    }

                    Vertex end = nodes[ei];
                    if (GeometryHelper.LengthSquared(end, x, y) <= epsSqr)
                    {
                        return (current, -1, ei);
                    }

                    if (new Rectangle(start, end).Contains(x, y))
                    {
                        return (current, worstEdge, -1);
                    }
                }

                if (worstCross > 0)
                {
                    // inside
                    return (current, -1, -1);
                }

                if (bestExit == -1)
                {
                    // outside and hit boundary
                    return (-1, -1, -1);
                }

                current = bestExit;
            }
            return (-1, -1, -1);
        }

        public static void SetAdjacent(Mesh mesh, int adjIndex, int adjStart, int adjEnd, int value)
        {
            if (adjIndex == -1) return;

            Triangle t = mesh.Triangles[adjIndex];
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

            mesh.Triangles[adjIndex] = t;
        }

        #region splitting
        public static int Split(Mesh mesh, int triangleIndex, int edgeIndex, int vertexIndex)
        {
            if (edgeIndex != -1)
            {
                return SplitEdge(mesh, triangleIndex, edgeIndex, vertexIndex);
            }
            return SplitTriangle(mesh, triangleIndex, vertexIndex);
        }

        public static int SplitTriangle(Mesh mesh, int t0, int vertexIndex)
        {
            List<Triangle> triangles = mesh.Triangles;
            List<Vertex> vertices = mesh.Vertices;
            List<Circle> circles = mesh.Circles;

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
                old.con0, -1, -1);

            circles.Add(Circle.From3Points(v1, v2, v3));
            triangles.Add(new Triangle(i1, i2, i3,
                old.adj1, t2, t0,
                old.con1, -1, -1));

            circles.Add(Circle.From3Points(v2, v0, v3));
            triangles.Add(new Triangle(i2, i0, i3,
               old.adj2, t0, t1,
               old.con2, -1, -1));

            SetAdjacent(mesh, old.adj1, i2, i1, t1);
            SetAdjacent(mesh, old.adj2, i0, i2, t2);

            v0.Triangle = v1.Triangle = v3.Triangle = t0;
            v2.Triangle = t1;

            mesh.NewTriangles[0] = t0;
            mesh.NewTriangles[1] = t1;
            mesh.NewTriangles[2] = t2;
            return 3;
        }

        public static int SplitEdge(Mesh mesh, int t0, int edgeIndex, int vertexIndex)
        {
            List<Triangle> triangles = mesh.Triangles;
            List<Vertex> vertices = mesh.Vertices;
            List<Circle> circles = mesh.Circles;

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
                    old0.con2, con, -1);

                circles.Add(Circle.From3Points(v2, v1, v3));
                triangles.Add(new Triangle(i2, i1, i3,
                    old0.adj1, t0, -1,
                    old0.con1, -1, con));

                SetAdjacent(mesh, old0.adj1, i2, i1, t1);

                v0.Triangle = v2.Triangle = v3.Triangle = t0;
                v1.Triangle = t1;

                mesh.NewTriangles[0] = t0;
                mesh.NewTriangles[1] = t1;

                if (con != -1)
                {
                    Split(mesh, con, i3, out TriangleEdge a, out TriangleEdge b);
                    a.triangle = t0;
                    b.triangle = t1;

                    mesh.Edges[con] = a;
                    mesh.Edges.Add(b);
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
                    old0.con2, con, -1);

                circles[t1] = Circle.From3Points(v0, v4, v3);
                triangles[t1] = new Triangle(i0, i4, i3,
                    old1.adj1, t2, t0,
                    old1.con1, -1, con);

                circles.Add(Circle.From3Points(v4, v1, v3));
                triangles.Add(new Triangle(i4, i1, i3,
                    old1.adj2, t3, t1,
                    old1.con2, con, -1));

                circles.Add(Circle.From3Points(v1, v2, v3));
                triangles.Add(new Triangle(i1, i2, i3,
                    old0.adj1, t0, t2,
                    old0.con1, -1, con));

                SetAdjacent(mesh, old0.adj1, i2, i1, t3);
                SetAdjacent(mesh, old1.adj2, i1, i3, t2);

                v0.Triangle = v2.Triangle = t0;
                v1.Triangle = t3;
                v3.Triangle = t1;

                mesh.NewTriangles[0] = t0;
                mesh.NewTriangles[1] = t1;
                mesh.NewTriangles[2] = t2;
                mesh.NewTriangles[3] = t3;

                con = old0.con0;
                if (con != -1)
                {
                    Split(mesh, con, i3, out TriangleEdge a, out TriangleEdge b);
                    a.triangle = t0;
                    b.triangle = t3;

                    mesh.Edges[con] = a;
                    mesh.Edges.Add(b);
                }

                con = old1.con0;
                if (con != -1)
                {
                    Split(mesh, con, i3, out TriangleEdge a, out TriangleEdge b);
                    a.triangle = t2;
                    b.triangle = t1;

                    mesh.Edges[con] = a;
                    mesh.Edges.Add(b);
                }
                return 4;
            }
        }

        static void Split(Mesh mesh, int edge, int vertex, out TriangleEdge a, out TriangleEdge b)
        {
            TriangleEdge e = mesh.Edges[edge];
            a = new TriangleEdge(e.id, e.start, vertex, -1);
            b = new TriangleEdge(e.id, vertex, e.end, -1);
        }
        #endregion splitting

        #region flipping
        public static bool CanFlip(Mesh mesh, int t0, int e0, out bool should)
        {
            should = false;

            List<Vertex> nodes = mesh.Vertices;
            List<Triangle> tris = mesh.Triangles;

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
            should = mesh.Circles[t0].Contains(v3.X, v3.Y);
            return GeometryHelper.IsConvex(v1, v2, v0, v3);
        }

        public static int Flip(Mesh mesh, int t0, int e0, bool forceFlip)
        {
            List<Triangle> triangles = mesh.Triangles;
            List<Vertex> vertices = mesh.Vertices;
            List<Circle> circles = mesh.Circles;

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

            // the first diagonal MUST be opposite to v2 to avoid degeneracy
            // 1) 0-3-2
            // 2) 3-1-2
            circles[t0] = Circle.From3Points(v0, v3, v2);
            triangles[t0] = new Triangle(i0, i3, i2,
                 old1.adj1, t1, old0.adj2,
                 old1.con1, con, old0.con2);

            circles[t1] = Circle.From3Points(v3, v1, v2);
            triangles[t1] = new Triangle(i3, i1, i2,
                old1.adj2, old0.adj1, t0,
                old1.con2, old0.con1, con);

            // as a result two triangles have 'lost' their neigbours
            // namely: t1(v0-v3) & t0 (v1-v2)
            SetAdjacent(mesh, old0.adj1, i3, i0, t0);
            SetAdjacent(mesh, old1.adj1, i1, i2, t1);

            v0.Triangle = v2.Triangle = v3.Triangle = t0;
            v1.Triangle = t1;

            mesh.NewTriangles[0] = t0;
            mesh.NewTriangles[1] = t1;

            if (con != -1)
            {
                TriangleEdge edge = mesh.Edges[con];
                mesh.Edges[con] = new TriangleEdge(edge.id, i3, i2, t0);
            }

            con = old1.con0;
            if (con != -1)
            {
                TriangleEdge edge = mesh.Edges[con];
                mesh.Edges[con] = new TriangleEdge(edge.id, i2, i3, t1);
            }
            return 2;
        }
        #endregion flipping

        #region legalization
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        public static int Legalize(Mesh mesh, ReadOnlySpan<int> indices, Stack<int>? affected = null)
        {
            // Stack ctor for span doesn't exist, so push manually.
            var stack = new Stack<int>(indices.Length);
            for (int i = 0; i < indices.Length; i++)
                stack.Push(indices[i]);

            return LegalizeCore(mesh, stack, affected);
        }

        public static int Legalize(Mesh mesh, int[] indices, int count, Stack<int>? affected = null)
        {
#if DEBUG
            if ((uint)count > (uint)indices.Length) throw new ArgumentOutOfRangeException(nameof(count));
#endif
            var stack = new Stack<int>(count);
            for (int i = 0; i < count; i++)
                stack.Push(indices[i]);

            return LegalizeCore(mesh, stack, affected);
        }

        static int LegalizeCore(Mesh mesh, Stack<int> stack, Stack<int>? affected)
        {
            int totalFlips = 0;
            List<Triangle> tris = mesh.Triangles;

            Dictionary<ulong, byte> flipCount = new Dictionary<ulong, byte>(64);
            while (stack.Count > 0)
            {
                int ti = stack.Pop();
                affected?.Push(ti);

                Triangle t = tris[ti];

                for (int ei = 0; ei < 3; ei++)
                {
                    t.Edge(ei, out int u, out int v);
                    ulong key = TriangleEdge.EdgeKey(u, v);

                    flipCount.TryGetValue(key, out byte flipsMade);
                    if (flipsMade >= MAX_FLIPS_PER_DIAGONAL)
                        continue;

                    if (!CanFlip(mesh, ti, ei, out bool should) || !should)
                        continue;

                    totalFlips++;
                    flipCount[key] = (byte)(flipsMade + 1);

                    int flippedCount = Flip(mesh, ti, ei, forceFlip: false);

                    for (int i = 0; i < flippedCount; i++)
                    {
                        int idx = mesh.NewTriangles[i];
                        stack.Push(idx);

                        if (affected is not null && ti != idx)
                            affected.Push(idx);
                    }
                    stack.Push(ti);
                    break;
                }
            }

            return totalFlips;
        }
        #endregion legalization

        #region insertion
        public static int TryInsert(Mesh mesh, Vertex vertex, double eps)
        {
            var (ti, ei, vi) = FindContaining(mesh, vertex.X, vertex.Y, eps);
            if (ti == -1) return -1;
            if (vi != -1) return vi;
            return Insert(mesh, vertex, ti, ei);
        }

        public static int Insert(Mesh mesh, Vertex vertex, int triangle, int edge, Stack<int>? affected = null)
        {
            int vertexIndex = mesh.Vertices.Count;
            mesh.Vertices.Add(vertex);

            int count = Split(mesh, triangle, edge, vertexIndex);
            int legalized = Legalize(mesh, mesh.NewTriangles, count, affected);
            return vertexIndex;
        }

        public static void Insert(Mesh mesh, string? id, Vertex start, Vertex end, double eps, bool alwaysSplit = false)
        {
            int si = TryInsert(mesh, start, eps);
            int ei = TryInsert(mesh, end, eps);
            if (si == -1 || ei == -1) return;
            Insert(mesh, id, si, ei, eps, alwaysSplit);
        }

        public static void Insert(Mesh mesh, string? id, int start, int end, double eps, bool alwaysSplit = false)
        {
            List<Vertex> nodes = mesh.Vertices;

            Queue<(int, int)> queue = new Queue<(int, int)>();
            queue.Enqueue((start, end));
            List<int> toLegalize = new List<int>();

            double epsSqr = eps * eps;
            while (queue.Count > 0)
            {
                var (s, e) = queue.Dequeue();
                Vertex startNode = nodes[s];
                Vertex endNode = nodes[e];

                if (GeometryHelper.LengthSquared(startNode, endNode) <= epsSqr) continue;

                int ti = OrientedEntranceTriangle(mesh, s, e, eps);
                Triangle t = mesh.Triangles[ti];

                if (SetConstraint(mesh, s, e, id))
                {
                    continue;
                }

                int next = t.vtx1;
                Vertex nextVertex = nodes[next];
                if (GeometryHelper.AreCollinear(startNode, endNode, nextVertex, eps))
                {
                    queue.Enqueue((s, next));
                    queue.Enqueue((next, e));
                    continue;
                }

                int prev = t.vtx2;
                Vertex prevVertex = nodes[prev];
                if (GeometryHelper.AreCollinear(startNode, endNode, prevVertex, eps))
                {
                    queue.Enqueue((s, prev));
                    queue.Enqueue((prev, e));
                    continue;
                }

                if (!GeometryHelper.Intersect(startNode, endNode, nextVertex, prevVertex, out double x, out double y))
                {
                    throw new Exception("Expected intersection");
                }

                Triangle adjacent = mesh.Triangles[t.adj1].Orient(prev, next);
                int opposite = adjacent.vtx2;

                int count;
                if (alwaysSplit || !CanFlip(mesh, ti, 1, out _))
                {
                    double seed1 = Vertex.Interpolate(startNode, endNode, x, y);
                    double seed2 = Vertex.Interpolate(nextVertex, prevVertex, x, y);
                    double seed = (seed1 + seed2) * 0.5;

                    Vertex vtx = new Vertex(null, x, y, seed);
                    int vtxIndex = mesh.Vertices.Count;
                    mesh.Vertices.Add(vtx);

                    count = SplitEdge(mesh, ti, 1, vtxIndex);

                    queue.Enqueue((s, vtxIndex));
                    queue.Enqueue((vtxIndex, opposite));
                    queue.Enqueue((opposite, e));
                }
                else
                {
                    count = Flip(mesh, ti, 1, false);

                    queue.Enqueue((s, opposite));
                    queue.Enqueue((opposite, e));
                }

                for (int i = 0; i < count; i++)
                    toLegalize.Add(mesh.NewTriangles[i]);
            }
            Legalize(mesh, CollectionsMarshal.AsSpan(toLegalize));
        }

        public static bool SetConstraint(Mesh mesh, int start, int end, string? id)
        {
            List<Vertex> nodes = mesh.Vertices;
            List<Triangle> tris = mesh.Triangles;

            Circler walker = new Circler(tris, nodes[start].Triangle, start);
            do
            {
                int ti = walker.Current;
                Triangle t = tris[ti];
                int edge = t.IndexOfInvariant(start, end);
                if (edge != -1)
                {
                    AddConstraint(mesh, ti, edge, id);
                    return true;
                }

            } while (walker.Next());

            return false;
        }

        static void AddConstraint(Mesh mesh, int triangle, int edge, string? id)
        {
            List<Triangle> tris = mesh.Triangles;
            Triangle tri = tris[triangle];

            int constraint = mesh.Edges.Count;

            int start, end, adjacent;
            switch (edge)
            {
                case 0:
                    tri.con0 = constraint;
                    adjacent = tri.adj0;
                    start = tri.vtx0;
                    end = tri.vtx1;
                    break;

                case 1:
                    tri.con1 = constraint;
                    adjacent = tri.adj1;
                    start = tri.vtx1;
                    end = tri.vtx2;
                    break;

                case 2:
                    tri.con2 = constraint;
                    adjacent = tri.adj2;
                    start = tri.vtx2;
                    end = tri.vtx0;
                    break;

                default:
                    throw new Exception();
            }

            mesh.Edges.Add(new TriangleEdge(id, start, end, triangle));
            tris[triangle] = tri;

            if (adjacent != -1)
            {
                Triangle triAdj = tris[adjacent];
                int otherEdge = triAdj.IndexOf(end, start);

                constraint = mesh.Edges.Count;
                switch (otherEdge)
                {
                    case 0:
                        triAdj.con0 = constraint;
                        break;

                    case 1:
                        triAdj.con1 = constraint;
                        break;

                    case 2:
                        triAdj.con2 = constraint;
                        break;

                    default:
                        throw new Exception();
                }

                mesh.Edges.Add(new TriangleEdge(id, end, start, adjacent));
                tris[adjacent] = triAdj;
            }
        }

        public static int OrientedEntranceTriangle(Mesh mesh, int start, int end, double eps)
        {
            List<Vertex> nodes = mesh.Vertices;
            List<Triangle> tris = mesh.Triangles;

            Vertex endVertex = nodes[end];

            Circler walker = new Circler(tris, nodes[start].Triangle, start);
            do
            {
                int tIndex = walker.Current;
                Triangle t = tris[walker.Current];
                t = t.Orient(t.IndexOf(start));

                if (GeometryHelper.Cross(nodes[t.vtx0], nodes[t.vtx1], endVertex) < -eps ||
                    GeometryHelper.Cross(nodes[t.vtx2], nodes[t.vtx0], endVertex) < -eps) continue;

                tris[tIndex] = t;
                return tIndex;

            } while (walker.Next());

            throw new Exception("Could not find entrance triangle.");
        }
        #endregion insertion

        #region refinement

        public static HashSet<int> BannedTriangles(Mesh mesh, Shape shape)
        {
            HashSet<int> banned = new HashSet<int>();
            int nTris = mesh.Triangles.Count;
            for (int ti = 0; ti < nTris; ti++)
            {
                Circle c = mesh.Circles[ti];
                if (!shape.Contains(c.x, c.y, 0))
                {
                    banned.Add(ti);
                }
            }
            return banned;
        }

      
        public static void Refine(Mesh mesh, Shape shape, double eps)
        {
            int super = 3;
            List<Vertex> nodes = mesh.Vertices;
            List<Triangle> tris = mesh.Triangles;
            List<TriangleEdge> edges = mesh.Edges;

            if (nodes.Count <= 3) return;

            QuadTree<Vertex> qt = new QuadTree<Vertex>(shape.Contour.Bounds, nodes.Count);
            for (int i = super; i < nodes.Count; i++)
                qt.Add(nodes[i]);

            if (qt.Count == 0) return;

            HashSet<ulong> seen = new HashSet<ulong>(capacity: Math.Max(64, edges.Count));
            Queue<int> triangleQueue = new Queue<int>(Math.Max(32, tris.Count / 2));
            Queue<ulong> segmentQueue = new Queue<ulong>(Math.Max(32, edges.Count));

            HashSet<int> banned = new HashSet<int>();
            for (int ti = 0; ti < tris.Count; ti++)
            {
                Circle c = mesh.Circles[ti];
                if (!shape.Contains(c.x, c.y, eps))
                {
                    banned.Add(ti);
                    continue;
                }

                if (BadTriangle(mesh, ti, super))
                {
                    triangleQueue.Enqueue(ti);
                }
            }

            for (int ei = 0; ei < edges.Count; ei++)
            {
                TriangleEdge edge = edges[ei];
                ulong key = TriangleEdge.EdgeKey(edge.start, edge.end);
                if (seen.Add(key) && Encroached(qt, nodes, key, eps))
                {
                    segmentQueue.Enqueue(key);
                }
            }

            ulong[] split = new ulong[2];
            Stack<int> affected = new Stack<int>(64);
            while (segmentQueue.Count > 0 || triangleQueue.Count > 0)
            {
                while (affected.Count > 0)
                    triangleQueue.Enqueue(affected.Pop());

                if (segmentQueue.Count > 0)
                {
                    ulong key = segmentQueue.Dequeue();
                    TriangleEdge.UnpackEdgeKey(key, out int start, out int end);
                    (int t, int e) = FindEdge(mesh, start, end, true);
                    if (e == -1)
                    {
                        seen.Remove(key);
                        continue;
                    }

                    Vertex vtx = Vertex.Between(nodes[start], nodes[end]);
                    int vtxIndex = Insert(mesh, vtx, t, e, affected);
                    qt.Add(vtx);

                    seen.Remove(key);

                    split[0] = TriangleEdge.EdgeKey(start, vtxIndex);
                    split[1] = TriangleEdge.EdgeKey(vtxIndex, end);

                    for (int i = 0; i < 2; i++)
                    {
                        ulong edge = split[i];
                        if (seen.Add(edge) && 
                            Encroached(qt, nodes, edge, eps) && 
                            VisibleFromInterior(edge, seen, nodes, vtx.X, vtx.Y))
                        {
                            segmentQueue.Enqueue(edge);
                        }
                    }
                    continue;
                }

                if (triangleQueue.Count > 0)
                {
                    int ti = triangleQueue.Dequeue();
                    if (!BadTriangle(mesh, ti, super)) continue;

                    Circle c = mesh.Circles[ti];
                    double x = c.x;
                    double y = c.y;
                    if (!qt.Bounds.Contains(x, y))
                    {
                        continue;
                    }

                    bool encroaches = false;
                    foreach (ulong seg in seen)
                    {
                        TriangleEdge.UnpackEdgeKey(seg, out int start, out int end);
                        Vertex a = nodes[start];
                        Vertex b = nodes[end];

                        if (Circle.From2Points(a, b).Contains(x, y) &&
                            VisibleFromInterior(seg, seen, nodes, x, y))
                        {
                            segmentQueue.Enqueue(seg);
                            encroaches = true;
                        }
                    }

                    if (encroaches)
                    {
                        continue;
                    }

                    Triangle t = tris[ti];
                    Vertex vtx = Vertex.Between(nodes[t.vtx0], nodes[t.vtx1], nodes[t.vtx2]);
                    vtx.X = x;
                    vtx.Y = y;

                    int inserted = TryInsert(mesh, vtx, eps);
                    if (inserted != -1)
                    {
                        qt.Add(vtx);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if the segment from this edge's diametral center to (x,y)
        /// does NOT intersect any other active constraint edge.
        /// </summary>
        public static bool VisibleFromInterior(
            ulong edge,
            HashSet<ulong> edges,
            List<Vertex> nodes,
            double x, double y)
        {
            TriangleEdge.UnpackEdgeKey(edge, out int eStart, out int eEnd);
            Circle dc = Circle.From2Points(nodes[eStart], nodes[eEnd]);
            Vertex center = new Vertex(null, dc.x, dc.y, -1);
            Vertex pt = new Vertex(null, x, y, -1);

            foreach (ulong key in edges)
            {
                if (key == edge) continue;
                TriangleEdge.UnpackEdgeKey(key, out int start, out int end);
                Vertex a = nodes[start];
                Vertex b = nodes[end];

                if (GeometryHelper.Intersect(center, pt, a, b, out _, out _))
                    return false;
            }
            return true;
        }

        public static bool Encroached(QuadTree<Vertex> qt, List<Vertex> nodes, ulong edge, double eps)
        {
            TriangleEdge.UnpackEdgeKey(edge, out int start, out int end);
            Circle c = Circle.From2Points(nodes[start], nodes[end]);
            List<Vertex> pts = qt.Query(new Rectangle(c));

            double epsSqr = eps * eps;
            Vertex a = nodes[start];
            Vertex b = nodes[end];

            for (int i = 0; i < pts.Count; i++)
            {
                Vertex p = pts[i];
                if (!c.Contains(p.X, p.Y)) continue;
                if (GeometryHelper.LengthSquared(a, p) <= epsSqr) continue;
                if (GeometryHelper.LengthSquared(b, p) <= epsSqr) continue;
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool BadTriangle(Mesh mesh, int ti, int super)
        {
            Triangle t = mesh.Triangles[ti];
            if (t.vtx0 < super || t.vtx1 < super || t.vtx2 < super)
            {
                return false;
            }

            var v0 = mesh.Vertices[t.vtx0];
            var v1 = mesh.Vertices[t.vtx1];
            var v2 = mesh.Vertices[t.vtx2];

            double targetArea = (v0.Seed + v1.Seed + v2.Seed) * (1.0 / 3.0);
            double area = Math.Abs(GeometryHelper.Cross(v0, v1, v2)) * 0.5;
            return area > targetArea;
        }
        #endregion refinement
    }
}
