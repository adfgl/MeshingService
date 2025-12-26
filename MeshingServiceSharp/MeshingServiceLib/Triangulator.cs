namespace MeshingServiceLib
{
    public sealed class Triangulator
    {
        public Mesh Mesh { get; private set; } = null!;

        public List<Vertex> Contour { get; set; } = new();
        public List<List<Vertex>>? Holes { get; set; }
        public List<Edge>? ConstrainedEdges { get; set; }
        public List<Vertex>? ConstrainedPoints { get; set; }

        public Triangulator Triangulate()
        {
            const double eps = 0;

            Polygon<Vertex> contour = ValidatePolygonOrThrow(Contour, "Contour");
            List<Polygon<Vertex>> holes = CollectValidHoles(contour, Holes, eps);

            Shape shape = new Shape(contour) { Holes = holes };

            var splitter = new EdgeSplitter(shape, eps);

            AddPolygonEdges(splitter, holes);
            AddEdges(splitter, ConstrainedEdges);
            AddPoints(splitter, ConstrainedPoints);

            ISuperStructure super = new SuperTriangle();
            Mesh mesh = super.Build(shape.Contour, scale: 2);

            foreach (Vertex p in splitter.Vertices)
                Mesh.TryInsert(mesh, p, eps);

            foreach (var seg in splitter.Segments)
                Mesh.Insert(mesh, seg.id, seg.a, seg.b, eps, alwaysSplit: false);

            // Optional: refinement step if you want it here
            // Mesh.Refine(mesh, shape, eps);

            Mesh = mesh;
            return this;
        }

        static Polygon<Vertex> ValidatePolygonOrThrow(List<Vertex> contour, string name)
        {
            var poly = new Polygon<Vertex>(contour);
            if (poly.IsSelfIntersecting())
                throw new InvalidOperationException($"{name} polygon is self-intersecting.");
            return poly;
        }

        static List<Polygon<Vertex>> CollectValidHoles(Polygon<Vertex> outer, List<List<Vertex>>? holesInput, double eps)
        {
            var holes = new List<Polygon<Vertex>>();
            if (holesInput is null) return holes;

            foreach (var ring in holesInput)
            {
                Polygon<Vertex> hole = ValidatePolygonOrThrow(ring, "Hole");

                // keep only holes that are relevant to outer (inside or intersecting)
                if (!outer.Contains(hole, eps) && !outer.Intersects(hole))
                    continue;

                bool drop = false;
                foreach (var other in holes)
                {
                    if (other.Contains(hole, eps))
                    {
                        drop = true;
                        break;
                    }
                }

                if (!drop)
                    holes.Add(hole);
            }

            return holes;
        }

        static void AddPolygonEdges(EdgeSplitter splitter, IEnumerable<Polygon<Vertex>> polygons)
        {
            foreach (var poly in polygons)
                foreach ((Vertex a, Vertex b) in poly.GetEdges())
                    splitter.Add(new Edge(a, b, id: null));
        }

        static void AddEdges(EdgeSplitter splitter, IEnumerable<Edge>? edges)
        {
            if (edges is null) return;
            foreach (var e in edges)
                splitter.Add(e);
        }

        static void AddPoints(EdgeSplitter splitter, IEnumerable<Vertex>? points)
        {
            if (points is null) return;
            foreach (var p in points)
                splitter.Add(p);
        }
    }


    public sealed class EdgeSplitter
    {
        readonly List<Edge> _segments = new();
        readonly List<Vertex> _vertices = new();

        readonly double _eps;
        readonly Shape _shape;

        public EdgeSplitter(Shape shape, double eps)
        {
            _eps = eps;
            _shape = shape;

            foreach ((Vertex a, Vertex b) in shape.Contour.GetEdges())
            {
                Add(new Edge(GetOrAddPoint(a), GetOrAddPoint(b), id: null));
            }
        }

        public IReadOnlyList<Edge> Segments => _segments;
        public IReadOnlyList<Vertex> Vertices => _vertices;

        public void Add(Vertex p)
        {
            if (!_shape.Contains(p.X, p.Y, _eps))
                return;

            Vertex cp = GetOrAddPoint(p);

            for (int i = 0; i < _segments.Count; i++)
            {
                Edge seg = _segments[i];

                // If point matches an endpoint, it's already merged/canonical.
                if (seg.ContainsEndpoint(cp, _eps))
                    return;

                // If point lies on this segment -> split it once and return.
                if (GeometryHelper.PointOnSegment(seg.a, seg.b, cp.X, cp.Y, _eps))
                {
                    _segments[i] = new Edge(seg.a, cp, seg.id);
                    AddSegmentIfValid(new Edge(cp, seg.b, seg.id));
                    return;
                }
            }
        }

        public void Add(Edge edge)
        {
            if (Vertex.Close(edge.a, edge.b, _eps))
                return;

            // Canonicalize endpoints (also merges ids)
            Vertex a = GetOrAddPoint(edge.a);
            Vertex b = GetOrAddPoint(edge.b);

            if (Vertex.Close(a, b, _eps))
                return;

            // Keep incoming id
            edge = new Edge(a, b, edge.id);

            var pending = new Queue<Edge>();
            pending.Enqueue(edge);

            while (pending.Count > 0)
            {
                Edge cur = pending.Dequeue();
                if (Vertex.Close(cur.a, cur.b, _eps))
                    continue;

                bool splitHappened = false;

                for (int i = 0; i < _segments.Count; i++)
                {
                    Edge existing = _segments[i];

                    // Try split cur against existing (and existing if needed).
                    // Returns 0 if no interaction.
                    var split = Split(cur, existing);
                    if (split.Count == 0)
                        continue;

                    // Existing segment got replaced by some pieces
                    _segments.RemoveAt(i);

                    // First 2 are always the "cur pieces" (re-queue)
                    EnqueueIfNonDegenerate(pending, split[0]);
                    EnqueueIfNonDegenerate(pending, split[1]);

                    // Remaining are the "existing pieces" (add now)
                    for (int k = 2; k < split.Count; k++)
                        AddSegmentIfValid(split[k]);

                    splitHappened = true;
                    break;
                }

                if (!splitHappened)
                {
                    AddSegmentIfValid(cur);
                }
            }
        }

        Vertex GetOrAddPoint(Vertex p)
        {
            if (!_shape.Contains(p.X, p.Y, _eps))
                return p;

            for (int i = 0; i < _vertices.Count; i++)
            {
                Vertex existing = _vertices[i];
                if (!Vertex.Close(existing, p, _eps))
                    continue;

                MergeId(existing, p);
                return existing;
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                Edge seg = _segments[i];

                if (Vertex.Close(seg.a, p, _eps))
                {
                    MergeId(seg.a, p);
                    return seg.a;
                }

                if (Vertex.Close(seg.b, p, _eps))
                {
                    MergeId(seg.b, p);
                    return seg.b;
                }
            }

            _vertices.Add(p);
            return p;
        }

        static void MergeId(Vertex target, Vertex incoming)
        {
            string? nid = incoming.Id;
            if (string.IsNullOrEmpty(nid))
                return;

            string? tid = target.Id;

            if (string.IsNullOrEmpty(tid) || tid.Length < nid.Length)
                target.Id = nid;
        }

        void AddSegmentIfValid(Edge s)
        {
            if (Vertex.Close(s.a, s.b, _eps))
                return;

            // Canonicalize endpoints
            Vertex a = GetOrAddPoint(s.a);
            Vertex b = GetOrAddPoint(s.b);
            if (Vertex.Close(a, b, _eps))
                return;

            // midpoint must be inside shape
            if (_shape.Contains((a.X + b.X) * 0.5, (a.Y + b.Y) * 0.5, _eps))
                _segments.Add(new Edge(a, b, s.id));
        }

        void EnqueueIfNonDegenerate(Queue<Edge> q, Edge s)
        {
            if (Vertex.Close(s.a, s.b, _eps))
                return;

            Vertex a = GetOrAddPoint(s.a);
            Vertex b = GetOrAddPoint(s.b);
            if (!Vertex.Close(a, b, _eps))
                q.Enqueue(new Edge(a, b, s.id));
        }

        List<Edge> Split(Edge cur, Edge existing)
        {
            Vertex a1 = cur.a, a2 = cur.b;
            Vertex b1 = existing.a, b2 = existing.b;

            // share endpoints -> ignore
            if (cur.ContainsEndpoint(b1, _eps) || cur.ContainsEndpoint(b2, _eps) ||
                existing.ContainsEndpoint(a1, _eps) || existing.ContainsEndpoint(a2, _eps))
            {
                return [];
            }

            // Existing endpoint lies on cur
            if (GeometryHelper.PointOnSegment(a1, a2, b1.X, b1.Y, _eps))
            {
                Vertex p = GetOrAddPoint(b1);
                return [
                    new Edge(a1, p, cur.id),
                    new Edge(p, a2, cur.id),
                    new Edge(p, b2, existing.id),
                ];
            }

            if (GeometryHelper.PointOnSegment(a1, a2, b2.X, b2.Y, _eps))
            {
                Vertex p = GetOrAddPoint(b2);
                return [
                    new Edge(a1, p, cur.id),
                new Edge(p, a2, cur.id),
                new Edge(b1, p, existing.id),
            ];
            }

            // Cur endpoint lies on existing
            if (GeometryHelper.PointOnSegment(b1, b2, a1.X, a1.Y, _eps))
            {
                Vertex p = GetOrAddPoint(a1);
                return [
                    new Edge(p, a2, cur.id),
                    new Edge(a1, p, cur.id), 
                    new Edge(b1, p, existing.id),
                    new Edge(p, b2, existing.id),
                ];
            }

            if (GeometryHelper.PointOnSegment(b1, b2, a2.X, a2.Y, _eps))
            {
                Vertex p = GetOrAddPoint(a2);
                return [
                    new Edge(a1, p, cur.id),
                    new Edge(p, a2, cur.id),
                    new Edge(b1, p, existing.id),
                    new Edge(p, b2, existing.id),
                ];
            }

            // Proper intersection
            if (GeometryHelper.Intersect(a1, a2, b1, b2, out double x, out double y))
            {
                double seed = Vertex.Interpolate(a1, a2, x, y);
                Vertex p = GetOrAddPoint(new Vertex(null, x, y, seed));

                return [
                    new Edge(a1, p, cur.id),
                new Edge(p, a2, cur.id),
                new Edge(b1, p, existing.id),
                new Edge(p, b2, existing.id),
            ];
            }

            return [];
        }

       
    }

    public readonly struct Edge
    {
        public readonly Vertex a;
        public readonly Vertex b;
        public readonly string? id;

        public Edge(Vertex a, Vertex b, string? id)
        {
            this.a = a;
            this.b = b;
            this.id = id;
        }

        public bool ContainsEndpoint(Vertex p, double eps)
            => Vertex.Close(a, p, eps) || Vertex.Close(b, p, eps);
    }
}
