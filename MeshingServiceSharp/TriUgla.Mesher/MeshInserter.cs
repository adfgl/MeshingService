using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class MeshInserter
    {
        readonly MeshProcessor _processor;
        readonly MeshLegalizer _legalizer;
        readonly MeshFinder _finder;

        public MeshInserter(Mesh mesh, MeshProcessor? processor = null)
        {
            _processor = processor ?? new MeshProcessor(mesh);
            _legalizer = new MeshLegalizer(mesh, _processor);
            _finder = new MeshFinder(mesh);
            Mesh = mesh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Triangle> Triangles() => CollectionsMarshal.AsSpan(Mesh.Triangles);

        public double Eps
        {
            get => field;
            set
            {
                if (field == value) return;
                _finder.Eps = field = value;
            }
        }
        public Mesh Mesh { get; set; }

        int _lastFound = -1;

        public int TryInsert(double x, double y, double seed, string? id)
        {
            var (ti, ei, vi) = _finder.FindContaining(x, y, _lastFound);
            _lastFound = ti;
            if (ti == -1) return -1;
            if (vi != -1) return vi;
            return Insert(x, y, seed, id, ti, ei);
        }

        public void TryInsert(
            double x0, double y0, double seed0, string? id0,
            double x1, double y1, double seed1, string? id1,
            string? id, bool alwaysSplit = false)
        {
            int si = TryInsert(x0, y0, seed0, id0);
            int ei = TryInsert(x1, y1, seed1, id1);
            if (si == -1 || ei == -1 || si == ei) return;
            Insert(id, si, ei, alwaysSplit);
        }

        public void Insert(string? id, int start, int end, bool alwaysSplit = false)
        {
            Queue<(int, int)> queue = new Queue<(int, int)>(8);
            List<int> toLegalize = new List<int>(128);

            queue.Enqueue((start, end));

            double eps = Eps;
            double epsSqr = eps * eps;

            Span<Vertex> verts = CollectionsMarshal.AsSpan(Mesh.Vertices.Items);
            Span<Triangle> tris = Triangles();

            while (queue.Count > 0)
            {
                var (a, b) = queue.Dequeue();
                var (ax, ay) = verts[a];
                var (bx, by) = verts[b];

                double abx = bx - ax;
                double aby = by - ay;
                if (abx * abx + aby * aby <= epsSqr)
                {
                    continue;
                }

                if (SetConstraint(a, b, id))
                {
                    // edge happens to exist already
                    continue;
                }

                int ti = OrientedEntranceTriangle(a, b);
                Triangle t = tris[ti];

                int next = t.vtx1;
                var (nx, ny) = verts[next];
                if (GeometryHelper.AreCollinear(ax, ay, bx, by, nx, ny, eps))
                {
                    SetConstraint(a, next, id);

                    queue.Enqueue((a, next));
                    queue.Enqueue((next, b));
                    continue;
                }

                int prev = t.vtx2;
                var (px, py) = verts[prev];
                if (GeometryHelper.AreCollinear(ax, ay, bx, by, px, py, eps))
                {
                    queue.Enqueue((a, prev));
                    queue.Enqueue((prev, b));
                    continue;
                }

                if (!GeometryHelper.Intersect(ax, ay, bx, by, nx, ny, px, py, out double x, out double y))
                {
                    throw new Exception("Expected intersection");
                }

                Triangle adjacent = tris[t.adj1];
                int edge = Triangle.IndexOf(in adjacent, prev, next);
                if (edge == -1)
                    throw new Exception($"Adjacent triangle {t.adj1} missing edge ({prev},{next}).");

                if (edge != 0)
                    adjacent = adjacent.Orient(edge);

                int opposite = adjacent.vtx2;

                int count;
                if (alwaysSplit || !_processor.CanFlip(ti, 1, out _))
                {
                    List<VertexMeta> metas = Mesh.Vertices.Meta;
                    double seed1 = GeometryHelper.Interpolate(
                            ax, ay, metas[a].seed,
                            bx, by, metas[b].seed,
                            x, y);

                    double seed2 = GeometryHelper.Interpolate(
                            nx, ny, metas[next].seed,
                            px, py, metas[prev].seed,
                            x, y);

                    double seed = (seed1 + seed2) * 0.5;

                    int vertexIndex = verts.Length;
                    Mesh.Vertices.Items.Add(new Vertex(x, y));
                    Mesh.Vertices.Meta.Add(new VertexMeta(-1, null, seed));
                    verts = CollectionsMarshal.AsSpan(Mesh.Vertices.Items);

                    count = _processor.SplitEdge(ti, 1, vertexIndex);

                    queue.Enqueue((a, vertexIndex));
                    queue.Enqueue((vertexIndex, opposite));
                    queue.Enqueue((opposite, b));

                }
                else
                {
                    count = _processor.Flip(ti, 1, false);

                    queue.Enqueue((a, opposite));
                    queue.Enqueue((opposite, b));
                }

                toLegalize.EnsureCapacity(count);
                for (int i = 0; i < count; i++)
                    toLegalize.Add(_processor.New[i]);
            }
            _legalizer.Legalize(CollectionsMarshal.AsSpan(toLegalize));
        }

        public int OrientedEntranceTriangle(int start, int end)
        {
            Span<Vertex> verts = CollectionsMarshal.AsSpan(Mesh.Vertices.Items);
            Span<Triangle> tris = Triangles();
            double eps = Eps;
            var (ex, ey) = verts[end];
            int startTri = Mesh.Vertices.Meta[start].triangle;
            Circler walker = new Circler(tris, startTri, start);
            do
            {
                int tIndex = walker.CurrentTriangle;
                Triangle t = tris[tIndex];

                int edge = Triangle.IndexOf(in t, start);

                int v0, v1, v2;
                switch (edge)
                {
                    case 0: v0 = t.vtx0; v1 = t.vtx1; v2 = t.vtx2; break;
                    case 1: v0 = t.vtx1; v1 = t.vtx2; v2 = t.vtx0; break;
                    case 2: v0 = t.vtx2; v1 = t.vtx0; v2 = t.vtx1; break;
                    default:
                        throw new InvalidOperationException($"Invalid edge index {edge} in triangle {tIndex}.");
                }

                Vertex a = verts[v0];
                Vertex b = verts[v1];
                Vertex c = verts[v2];

                if (GeometryHelper.Cross(a.x, a.y, b.x, b.y, ex, ey) < -eps) continue;
                if (GeometryHelper.Cross(c.x, c.y, a.x, a.y, ex, ey) < -eps) continue;

                if (edge != 0)
                {
                    tris[tIndex] = t.Orient(edge);
                }
                return tIndex;

            } while (walker.Next());

            throw new Exception("Could not find entrance triangle.");
        }

        public int Insert(double x, double y, double seed, string? id, int triangle, int edge, Stack<int>? affected = null)
        {
            int vertexIndex = Mesh.Vertices.Items.Count;
            Mesh.Vertices.Items.Add(new Vertex(x, y));
            Mesh.Vertices.Meta.Add(new VertexMeta(triangle, id, seed));

            int count = _processor.Split(triangle, edge, vertexIndex);
            int legalized = _legalizer.Legalize(_processor.New, count, affected);
            return vertexIndex;
        }

        public bool SetConstraint(int start, int end, string? id)
        {
            ReadOnlySpan<Triangle> tris = Triangles();
            int seed = Mesh.Vertices.Meta[start].triangle;
            Circler circler = new Circler(tris, seed, start);
            do
            {
                int ti = circler.CurrentTriangle;
                Triangle t = tris[ti];
                int edge = Triangle.IndexOfInvariant(in t, start, end);
                if (edge != -1)
                {
                    AddConstraint(ti, edge, id);
                    return true;
                }

            } while (circler.Next());

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddConstraint(int triangle, int edge, string? id)
        {
            if ((uint)edge > 2u) throw new ArgumentOutOfRangeException(nameof(edge));

            List<Triangle> tris = Mesh.Triangles;
            List<Edge> edges = Mesh.Edges.Items;
            List<EdgeMeta> metas = Mesh.Edges.Meta;

            int baseId = edges.Count;
            Triangle tri = tris[triangle];

            int start, end, adjacent;
            switch (edge)
            {
                case 0:
                    tri.con0 = baseId;
                    adjacent = tri.adj0;
                    start = tri.vtx0;
                    end = tri.vtx1;
                    break;

                case 1:
                    tri.con1 = baseId;
                    adjacent = tri.adj1;
                    start = tri.vtx1;
                    end = tri.vtx2;
                    break;

                default:
                    tri.con2 = baseId;
                    adjacent = tri.adj2;
                    start = tri.vtx2;
                    end = tri.vtx0;
                    break;
            }

            edges.Add(new Edge(start, end));
            metas.Add(new EdgeMeta(triangle, id));
            tris[triangle] = tri;

            if (adjacent == -1)
                return;

            Triangle triAdj = tris[adjacent];

            int otherEdge = Triangle.IndexOf(in triAdj, end, start);
            if ((uint)otherEdge > 2u)
                throw new InvalidOperationException("Adjacent triangle does not contain the shared edge (topology broken).");

            int backId = baseId + 1;
            switch (otherEdge)
            {
                case 0: triAdj.con0 = backId; break;
                case 1: triAdj.con1 = backId; break;
                default: triAdj.con2 = backId; break;
            }

            edges.Add(new Edge(end, start));
            metas.Add(new EdgeMeta(adjacent, id));
            tris[adjacent] = triAdj;
        }
    }
}
