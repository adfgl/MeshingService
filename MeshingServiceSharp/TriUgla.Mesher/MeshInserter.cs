using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class MeshInserter(Mesh mesh)
    {
        readonly MeshFinder _finder = new MeshFinder(mesh);

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
        public Mesh Mesh { get; set; } = mesh;

        int _lastFoundTriangle = -1;

        public int TryInsert(double x, double y, double seed, string? id)
        {
            SearchResult searchResult = _finder.FindContaining(x, y, _lastFoundTriangle);
            if (searchResult.status != SearchStatus.Found)
            {
                return -1;
            }

            _lastFoundTriangle = searchResult.triangle;
            if (searchResult.vertex != -1)
            {
                return searchResult.vertex;
            }
            return Insert(x, y, seed, id, _lastFoundTriangle, searchResult.edge);
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

        public void Insert(string? id, int startIndex, int endIndex, bool alwaysSplit = false)
        {
            Queue<(int, int)> queue = new Queue<(int, int)>(8);
            List<int> toLegalize = new List<int>(128);

            queue.Enqueue((startIndex, endIndex));

            double eps = Eps;
            double epsSqr = eps * eps;

            Span<Vertex> verts = CollectionsMarshal.AsSpan(Mesh.Vertices.Items);
            Span<Triangle> tris = Triangles();

            while (queue.Count > 0)
            {
                var (a, b) = queue.Dequeue();
                if (a == b) continue;

                if (SetConstraint(a, b, id))
                {
                    // edge happens to exist already
                    continue;
                }

                int ti = OrientedEntranceTriangle(a, b);
                ref readonly Triangle t = ref tris[ti];

                int currIndex = t.vtx0;
                int nextIndex = t.vtx1;
                int prevIndex = t.vtx2;

                Debug.Assert(currIndex == a);

                ref readonly Vertex start = ref verts[currIndex];
                ref readonly Vertex next = ref verts[nextIndex];
                ref readonly Vertex prev = ref verts[prevIndex];
                ref readonly Vertex end = ref verts[b];

                if (GeometryHelper.AreCollinear(in start, in end, in next, eps))
                {
                    queue.Enqueue((a, nextIndex));
                    queue.Enqueue((nextIndex, b));
                    continue;
                }

                if (GeometryHelper.AreCollinear(in start, in end, in prev, eps))
                {
                    queue.Enqueue((a, prevIndex));
                    queue.Enqueue((prevIndex, b));
                    continue;
                }

                if (!Intersection.Intersect(in start, in end, in next, in prev, out Vertex inter))
                {
                    throw new Exception("Expected intersection");
                }

                int opposite = Triangle.VertexIndexOppositeToEdge(tris, ti, nextIndex, prevIndex);

                int count;
                if (alwaysSplit || !_processor.CanFlip(ti, 1, out _))
                {
                    int vertexIndex = verts.Length;
                    Mesh.Vertices.Items.Add(inter);
                    Mesh.Vertices.Meta.Add(new VertexMeta(-1, null));
                    verts = CollectionsMarshal.AsSpan(Mesh.Vertices.Items);

                    count = _processor.SplitEdge(ti, 1, vertexIndex);

                    queue.Enqueue((a, vertexIndex));
                    queue.Enqueue((vertexIndex, opposite));
                    queue.Enqueue((opposite, b));

                }
                else
                {
                    count = _processor.FlipCCW(ti, 1);

                    queue.Enqueue((a, opposite));
                    queue.Enqueue((opposite, b));
                }

                toLegalize.EnsureCapacity(count);
                for (int i = 0; i < count; i++)
                    toLegalize.Add(_processor.New[i]);
            }
            _legalizer.Legalize(CollectionsMarshal.AsSpan(toLegalize));
        }

        public int OrientedEntranceTriangle(int startIndex, int endIndex)
        {
            Span<Vertex> verts = CollectionsMarshal.AsSpan(Mesh.Vertices.Items);
            Span<Triangle> tris = Triangles();
            double eps = Eps;
            ref readonly Vertex end = ref verts[endIndex];
            int startTri = Mesh.Vertices.Meta[startIndex].triangle;
            Circler walker = new Circler(tris, startTri, startIndex);
            do
            {
                int tIndex = walker.CurrentTriangle;
                Triangle t = tris[tIndex];

                int edge = Triangle.IndexOf(in t, startIndex);

                int v0, v1, v2;
                switch (edge)
                {
                    case 0: v0 = t.vtx0; v1 = t.vtx1; v2 = t.vtx2; break;
                    case 1: v0 = t.vtx1; v1 = t.vtx2; v2 = t.vtx0; break;
                    case 2: v0 = t.vtx2; v1 = t.vtx0; v2 = t.vtx1; break;
                    default:
                        throw new InvalidOperationException($"Invalid edge index {edge} in triangle {tIndex}.");
                }

                ref readonly Vertex a = ref verts[v0];
                ref readonly Vertex b = ref verts[v1];
                ref readonly Vertex c = ref verts[v2];

                if (GeometryHelper.Cross(in a, in b, in end) < -eps) continue;
                if (GeometryHelper.Cross(in c, in a, in end) < -eps) continue;

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
            Mesh.Vertices.Items.Add(new Vertex(x, y, seed));
            Mesh.Vertices.Meta.Add(new VertexMeta(triangle, id));

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
            List<Edge> edges = Mesh.Edges;

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

            edges.Add(new Edge(start, end, triangle, id));
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

            edges.Add(new Edge(end, start, adjacent, id));
            tris[adjacent] = triAdj;
        }
    }
}
