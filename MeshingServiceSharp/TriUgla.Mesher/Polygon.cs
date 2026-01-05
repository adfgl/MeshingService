using System.Runtime.CompilerServices;

namespace TriUgla.Mesher
{
    public sealed class Polygon
    {
        readonly List<Vertex> _vts;
        readonly Rectangle _rect;

        public Polygon(IEnumerable<Vertex> vertices)
        {
            double eps = 1e-6;
            
            _vts = new List<Vertex>();
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (Vertex vertex in vertices)
            {
                if (!_vtx.Any(existing => 
                    Vertex.Close(in existing, in vertex, eps))
                {
                    _vts.Add(vertex);

                    double x = vertex.x;
                    double y = vertex.y;
                    if (x < minX) minX = x;
                    if (y < minY) minY = y;
                    if (x > maxX) maxX = x;
                    if (y > maxY) maxY = y;
                }
            }

            if (_vts.Count < 3)
                throw new ArgumentException("Invalid polygon: need at least 3 unique vertices.", nameof(vertices));

            Vertex first = _vts[0];
            Vertex last = _vts[^1];
            if (!Vertex.Close(in first, in close, eps))
            {
                _vts.Add(first);
            }

            _rect = new Rectangle(minX, minY, maxX, maxY);
        }

        public Rectangle Bounds => _rect;
        public IReadOnlyList<Vertex> Vertices => _vts;

        public bool Contains(double x, double y, double eps)
        {
            return _rect.Contains(x, y) && Contains(_vts, x, y, eps);
        }

        public bool ContainsOrIntersects(Polygon other, double eps)
            => Contains(other, eps) || Intersects(other);

        public bool Contains(Polygon other, double eps)
        {
            if (_rect.Contains(other._rect))
            {
                for (int i = 0; i < other._vts.Count - 1; i++)
                {
                    var (x, y) = other._vts[i];
                    if (!Contains(x, y, eps))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool Intersects(Polygon other)
        {
            if (!_rect.Intersects(other._rect)) return false;

            List<Vertex> a = _vts;
            List<Vertex> b = other._vts;

            int aEdges = a.Count - 1;
            int bEdges = b.Count - 1;
            for (int i = 0; i < aEdges; i++)
            {
                var (p1x, p1y) = a[i];
                var (q1x, q1y) = a[i + 1];
                for (int j = 0; j < bEdges; j++)
                {
                    Vertex p2 = b[j];
                    Vertex q2 = b[j + 1];
                    if (GeometryHelper.Intersect(
                        p1x, p1y, q1x, q1y,
                        p2.x, p2.y, q2.x, q2.y,
                        out _, out _))
                        return true;
                }
            }

            return false;

        }

        public static bool Contains(List<Vertex> poly, double x, double y, double eps)
        {
            int n = poly.Count;
            if (n < 4) return false;

            bool inside = false;
            for (int i = 0; i < n - 1; i++)
            {
                var a = poly[i];
                var b = poly[i + 1];

                double ax = a.x, ay = a.y;
                double bx = b.x, by = b.y;

                if (GeometryHelper.PointOnSegment(ax, ay, bx, by, x, y, eps))
                    return true;

                double dy = by - ay;
                if (Math.Abs(dy) <= eps) continue;

                double ymin = ay < by ? ay : by;
                double ymax = ay < by ? by : ay;

                if (y < ymin - eps || y >= ymax - eps) continue;

                double dx = bx - ax;

                double lhs = (x - ax) * dy;
                double rhs = dx * (y - ay);

                bool cross = dy > 0
                    ? (rhs > lhs + eps * Math.Abs(dy))
                    : (rhs < lhs - eps * Math.Abs(dy));

                if (cross) inside = !inside;
            }
            return inside;
        }
    }
}
