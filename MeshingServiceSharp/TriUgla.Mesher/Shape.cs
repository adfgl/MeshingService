namespace TriUgla.Mesher
{
    public sealed class Shape
    {
        readonly List<Polygon> _contours;
        readonly List<Polygon> _holes;

        public Shape(List<Polygon> contours, List<Polygon>? holes = null, double eps = 1e-6)
        {
            if (contours.Count == 0)
            {
                throw new ArgumentException("Need at least one contour.");
            }

            List<Polygon> holesKeep;
            if (holes is null)
            {
                holesKeep = new List<Polygon>();
            }
            else
            {
                holesKeep = new List<Polygon>(holes.Count);
                foreach (Polygon hole in holes)
                {
                    if (contours.Any(o => o.ContainsOrIntersects(hole, eps)) &&
                        !holesKeep.Any(o => o.Contains(hole, eps)))
                    {
                        holesKeep.Add(hole);
                    }
                }
            }
            _holes = holesKeep;
            _contours = contours;
            Eps = eps;  
        }

        public List<Polygon> Contours => _contours;
        public List<Polygon> Holes => _holes;
        public double Eps { get; set; }

        public bool Contains(double x, double y) =>
                Contours.Any(hole => hole.Contains(x, y, Eps)) &&
                !Holes.Any(hole => hole.Contains(x, y, Eps));
    }
}
