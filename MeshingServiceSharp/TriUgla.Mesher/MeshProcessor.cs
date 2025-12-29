using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class MeshProcessor(Mesh mesh)
    {
        readonly int[] s_new = new int[4];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Triangle> Triangles() => CollectionsMarshal.AsSpan(mesh.Triangles);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Vertex> Vertices() => CollectionsMarshal.AsSpan(mesh.Vertices.Items);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Circle> Circles() => CollectionsMarshal.AsSpan(mesh.Circles);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<VertexMeta> VertexMeta() => CollectionsMarshal.AsSpan(mesh.Vertices.Meta);

        public IReadOnlyList<int> New => s_new;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetAdjacent(int adjIndex, int adjStart, int adjEnd, int value)
        {
            Span<Triangle> tris = Triangles();
            if ((uint)adjIndex >= (uint)tris.Length) return; // -1 or out of range: ignore

            ref Triangle t = ref tris[adjIndex];
            int e = Triangle.IndexOf(in t, adjStart, adjEnd);
            if (e < 0)
            {
                throw new InvalidOperationException(
                    $"Triangle {adjIndex} does not contain directed edge ({adjStart}->{adjEnd}). " +
                    $"Edges: ({t.vtx0}->{t.vtx1}), ({t.vtx1}->{t.vtx2}), ({t.vtx2}->{t.vtx0}).");
            }

            if (e == 0) t.adj0 = value;
            else if (e == 1) t.adj1 = value;
            else t.adj2 = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ReplaceConstraintSegment(int conIndex, int start, int end, int leftTriangle)
        {
            if (conIndex < 0) return;

            List<Edge> items = mesh.Edges.Items;
            List<EdgeMeta> metas = mesh.Edges.Meta;

            EdgeMeta em = metas[conIndex];
            items[conIndex] = new Edge(start, end);
            metas[conIndex] = new EdgeMeta(leftTriangle, em.id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SplitConstraintEdge(int conIndex, int newVertex, int leftTriangle, int rightTriangle)
        {
            if (conIndex < 0) return;

            List<Edge> edgeItems = mesh.Edges.Items;
            List<EdgeMeta> edgeMeta = mesh.Edges.Meta;

            Edge e = edgeItems[conIndex];
            EdgeMeta em = edgeMeta[conIndex];

            edgeItems[conIndex] = new Edge(e.start, newVertex);
            edgeMeta[conIndex] = new EdgeMeta(leftTriangle, em.id);

            edgeItems.Add(new Edge(newVertex, e.end));
            edgeMeta.Add(new EdgeMeta(rightTriangle, em.id));
        }

        #region FLIPPING
        public bool CanFlip(int triangleIndex, int edgeIndex, out bool should)
        {
            should = false;

            Triangle t0 = mesh.Triangles[triangleIndex].Orient(edgeIndex);
            int adj = t0.adj0;
            if (adj < 0) return false;

            Triangle t1raw = mesh.Triangles[adj];
            int e1 = Triangle.IndexOf(in t1raw, t0.vtx1, t0.vtx0);
#if DEBUG
            if ((uint)e1 > 2u) throw new Exception("Broken adjacency / IndexOf failed.");
#endif
            int i3 = (e1 == 0) ? t1raw.vtx2 : (e1 == 1) ? t1raw.vtx0 : t1raw.vtx1;

            Span<Vertex> v = Vertices();
            double x0 = v[t0.vtx0].x, y0 = v[t0.vtx0].y;
            double x1 = v[t0.vtx1].x, y1 = v[t0.vtx1].y;
            double x2 = v[t0.vtx2].x, y2 = v[t0.vtx2].y;
            double x3 = v[i3].x,      y3 = v[i3].y;

            if (!GeometryHelper.IsConvex(x1, y1, x2, y2, x0, y0, x3, y3))
                return false;

            if (t0.con0 < 0)
                should = mesh.Circles[triangleIndex].Contains(x3, y3);
            return true;
        }

        public int Flip(int triangleIndex, int edgeIndex, bool forceFlip)
        {
            Span<Triangle> tris = Triangles();
            Span<Circle> crcs = Circles();

            Triangle a = tris[triangleIndex].Orient(edgeIndex);
            int t1 = a.adj0;
            if (t1 < 0) return 0;

            // Respect constraint unless forced
            if (a.con0 >= 0 && !forceFlip) return 0;

            Triangle bRaw = tris[t1];
            int e1 = Triangle.IndexOf(in bRaw, a.vtx1, a.vtx0);
#if DEBUG
            if ((uint)e1 > 2u) throw new Exception("Broken adjacency / IndexOf failed.");
#endif
            Triangle b = bRaw.Orient(e1);

            int i0 = a.vtx0, i1v = a.vtx1, i2 = a.vtx2, i3 = b.vtx2;

            Span<Vertex> v = Vertices();
            double x0 = v[i0].x, y0 = v[i0].y;
            double x1 = v[i1v].x, y1 = v[i1v].y;
            double x2 = v[i2].x, y2 = v[i2].y;
            double x3 = v[i3].x, y3 = v[i3].y;

            // New diagonal is (i2 <-> i3)
            // t0 becomes (i0, i3, i2)
            crcs[triangleIndex] = Circle.From3Points(x0, y0, x3, y3, x2, y2);
            tris[triangleIndex] = new Triangle(i0, i3, i2,
                b.adj1, t1, a.adj2,
                b.con1, -1, a.con2);

            // t1 becomes (i3, i1, i2)
            crcs[t1] = Circle.From3Points(x3, y3, x1, y1, x2, y2);
            tris[t1] = new Triangle(i3, i1v, i2,
                b.adj2, a.adj1, triangleIndex,
                b.con2, a.con1, -1);

            SetAdjacent(a.adj1, i2, i1v, t1);         
            SetAdjacent(b.adj1, i3, i0, triangleIndex);

            Span<VertexMeta> meta = VertexMeta();
            meta[i0].triangle = triangleIndex;
            meta[i2].triangle = triangleIndex;
            meta[i3].triangle = triangleIndex;
            meta[i1v].triangle = t1;

            ReplaceConstraintSegment(a.con0, i3, i2, triangleIndex);
            ReplaceConstraintSegment(b.con0, i2, i3, t1);

            s_new[0] = triangleIndex;
            s_new[1] = t1;
            return 2;
        }
        #endregion FLIPPING

        #region SPLITTING
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Split(int triangleIndex, int edgeIndex, int vertexIndex)
            => edgeIndex < 0 ? SplitTriangle(triangleIndex, vertexIndex)
                             : SplitEdge(triangleIndex, edgeIndex, vertexIndex);

        public int SplitTriangle(int triangleIndex, int vertexIndex)
        {
            List<Triangle> tris = mesh.Triangles;
            List<Circle> crcs = mesh.Circles;

            int t0 = triangleIndex;
            int t1 = tris.Count;
            int t2 = t1 + 1;

            Triangle old = tris[t0];
            int i0 = old.vtx0, i1 = old.vtx1, i2 = old.vtx2, i3 = vertexIndex;

            Span<Vertex> v = Vertices();
            double x0 = v[i0].x, y0 = v[i0].y;
            double x1 = v[i1].x, y1 = v[i1].y;
            double x2 = v[i2].x, y2 = v[i2].y;
            double x3 = v[i3].x, y3 = v[i3].y;

            // t0: (i0,i1,i3)
            crcs[t0] = Circle.From3Points(x0, y0, x1, y1, x3, y3);
            tris[t0] = new Triangle(i0, i1, i3,
                old.adj0, t1, t2,
                old.con0, -1, -1);

            // t1: (i1,i2,i3)
            crcs.Add(Circle.From3Points(x1, y1, x2, y2, x3, y3));
            tris.Add(new Triangle(i1, i2, i3,
                old.adj1, t2, t0,
                old.con1, -1, -1));

            // t2: (i2,i0,i3)
            crcs.Add(Circle.From3Points(x2, y2, x0, y0, x3, y3));
            tris.Add(new Triangle(i2, i0, i3,
                old.adj2, t0, t1,
                old.con2, -1, -1));

            SetAdjacent(old.adj1, i2, i1, t1);
            SetAdjacent(old.adj2, i0, i2, t2);

            Span<VertexMeta> meta = VertexMeta();
            meta[i0].triangle = t0;
            meta[i1].triangle = t0;
            meta[i3].triangle = t0;
            meta[i2].triangle = t1;

            s_new[0] = t0;
            s_new[1] = t1;
            s_new[2] = t2;
            return 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int SplitEdge(int triangleIndex, int edgeIndex, int vertexIndex)
        {
            Triangle old0 = mesh.Triangles[triangleIndex].Orient(edgeIndex);
            return old0.adj0 < 0
                ? SplitEdgeNoAdjacent(in old0, triangleIndex, vertexIndex)
                : SplitEdgeHasAdjacent(in old0, triangleIndex, vertexIndex);
        }

        int SplitEdgeHasAdjacent(in Triangle old0, int triangleIndex, int vertexIndex)
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

            List<Triangle> tris = mesh.Triangles;
            List<Circle> crcs = mesh.Circles;

            int t0 = triangleIndex;
            int t1 = old0.adj0;
            int t2 = tris.Count;
            int t3 = t2 + 1;

            int i0 = old0.vtx0, i1 = old0.vtx1, i2 = old0.vtx2, i3 = vertexIndex;
            int con = old0.con0;

            Triangle old1 = tris[t1];
            old1 = old1.Orient(Triangle.IndexOf(in old1, i1, i0));
            int i4 = old1.vtx2;

            Span<Vertex> v = Vertices();
            double x0 = v[i0].x, y0 = v[i0].y;
            double x1 = v[i1].x, y1 = v[i1].y;
            double x2 = v[i2].x, y2 = v[i2].y;
            double x3 = v[i3].x, y3 = v[i3].y;
            double x4 = v[i4].x, y4 = v[i4].y;

            // t0: (i2,i0,i3)
            crcs[t0] = Circle.From3Points(x2, y2, x0, y0, x3, y3);
            tris[t0] = new Triangle(i2, i0, i3,
                old0.adj2, t1, t3,
                old0.con2, con, -1);

            // t1: (i0,i4,i3)
            crcs[t1] = Circle.From3Points(x0, y0, x4, y4, x3, y3);
            tris[t1] = new Triangle(i0, i4, i3,
                old1.adj1, t2, t0,
                old1.con1, -1, con);

            // t2: (i4,i1,i3)
            crcs.Add(Circle.From3Points(x4, y4, x1, y1, x3, y3));
            tris.Add(new Triangle(i4, i1, i3,
                old1.adj2, t3, t1,
                old1.con2, con, -1));

            // t3: (i1,i2,i3)
            crcs.Add(Circle.From3Points(x1, y1, x2, y2, x3, y3));
            tris.Add(new Triangle(i1, i2, i3,
                old0.adj1, t0, t2,
                old0.con1, -1, con));

            // Patch neighbors on the two "outer" edges.
            SetAdjacent(old0.adj1, i2, i1, t3);
            SetAdjacent(old1.adj2, i1, i3, t2);

            Span<VertexMeta> meta = VertexMeta();
            meta[i0].triangle = t0;
            meta[i2].triangle = t0;
            meta[i1].triangle = t3;
            meta[i3].triangle = t1;

            // Split constraint segments on both sides of the shared edge.
            SplitConstraintEdge(old0.con0, i3, t0, t3);
            SplitConstraintEdge(old1.con0, i3, t2, t1);

            s_new[0] = t0;
            s_new[1] = t1;
            s_new[2] = t2;
            s_new[3] = t3;
            return 4;
        }

        int SplitEdgeNoAdjacent(in Triangle old0, int triangleIndex, int vertexIndex)
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

            List<Triangle> tris = mesh.Triangles;
            List<Circle> crcs = mesh.Circles;

            int t0 = triangleIndex;
            int t1 = tris.Count;

            int i0 = old0.vtx0, i1 = old0.vtx1, i2 = old0.vtx2, i3 = vertexIndex;
            int con = old0.con0;

            Span<Vertex> v = Vertices();
            double x0 = v[i0].x, y0 = v[i0].y;
            double x1 = v[i1].x, y1 = v[i1].y;
            double x2 = v[i2].x, y2 = v[i2].y;
            double x3 = v[i3].x, y3 = v[i3].y;

            // t0: (i2,i0,i3)
            crcs[t0] = Circle.From3Points(x2, y2, x0, y0, x3, y3);
            tris[t0] = new Triangle(i2, i0, i3,
                old0.adj2, -1, t1,
                old0.con2, con, -1);

            // t1: (i1,i2,i3)
            crcs.Add(Circle.From3Points(x1, y1, x2, y2, x3, y3));
            tris.Add(new Triangle(i1, i2, i3,
                old0.adj1, t0, -1,
                old0.con1, -1, con));

            SetAdjacent(old0.adj1, i2, i1, t1);

            Span<VertexMeta> meta = VertexMeta();
            meta[i0].triangle = t0;
            meta[i2].triangle = t0;
            meta[i3].triangle = t0;
            meta[i1].triangle = t1;

            SplitConstraintEdge(con, i3, t0, t1);

            s_new[0] = t0;
            s_new[1] = t1;
            return 2;
        }
        #endregion SPLITTING

        #region LEGALIZATION
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Key(int a, int b)
        {
            uint lo = (uint)(a < b ? a : b);
            uint hi = (uint)(a < b ? b : a);
            return ((ulong)hi << 32) | lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unpack(ulong key, out int a, out int b)
        {
            uint lo = (uint)(key & 0xFFFFFFFF);
            uint hi = (uint)(key >> 32);

            a = (int)lo;
            b = (int)hi;
        }

        public int Legalize(ReadOnlySpan<int> indices, Stack<int>? affected = null, Stack<int>? work = null)
        {
            Stack<int> stack = work ?? new Stack<int>(indices.Length);
            if (work is null)
            {
                for (int i = 0; i < indices.Length; i++)
                    stack.Push(indices[i]);
            }
            else
            {
                stack.Clear();
                for (int i = 0; i < indices.Length; i++)
                    stack.Push(indices[i]);
            }

            return LegalizeCore(stack, affected);
        }

        public int Legalize(int[] indices, int count, Stack<int>? affected = null, Stack<int>? work = null)
        {
#if DEBUG
            if ((uint)count > (uint)indices.Length) throw new ArgumentOutOfRangeException(nameof(count));
#endif
            Stack<int> stack = work ?? new Stack<int>(count);
            if (work is null)
            {
                for (int i = 0; i < count; i++)
                    stack.Push(indices[i]);
            }
            else
            {
                stack.Clear();
                for (int i = 0; i < count; i++)
                    stack.Push(indices[i]);
            }

            return LegalizeCore(stack, affected);
        }

        int LegalizeCore(Stack<int> stack, Stack<int>? affected)
        {
            int totalFlips = 0;

            Dictionary<ulong, byte> flipCount = new Dictionary<ulong, byte>(64);
            Span<Triangle> tris = Triangles();
            while (stack.Count > 0)
            {
                int ti = stack.Pop();
                if ((uint)ti >= (uint)tris.Length) continue;

                affected?.Push(ti);

                Triangle t = tris[ti];
                for (int ei = 0; ei < 3; ei++)
                {
                    int u = ei == 0 ? t.vtx0 : (ei == 1 ? t.vtx1 : t.vtx2);
                    int v = ei == 0 ? t.vtx1 : (ei == 1 ? t.vtx2 : t.vtx0);

                    ulong key = Key(u, v);
                    flipCount.TryGetValue(key, out byte flipsMade);
                    if (flipsMade >= MAX_FLIPS_PER_DIAGONAL)
                        continue;

                    if (!CanFlip(ti, ei, out bool should) || !should)
                        continue;

                    int changed = Flip(ti, ei, forceFlip: false);
                    if (changed == 0) continue;

                    totalFlips++;
                    flipCount[key] = (byte)(flipsMade + 1);

                    for (int i = 0; i < changed; i++)
                    {
                        int idx = s_new[i];
                        stack.Push(idx);

                        if (affected is not null && idx != ti)
                            affected.Push(idx);
                    }

                    stack.Push(ti);
                    break;
                }
            }

            return totalFlips;
        }
        #endregion LEGALIZATION
    }
}
