namespace TriUgla.Mesher
{
    public sealed class Shape
    {
        readonly List<Polygon> _contours;
        readonly List<Polygon> _holes;

        public static List<Polygon> ExctractValidHoles(List<Polygon> contours, List<Polyon> holes, double eps)
        {
            List<Polygon> toKeep = new List<Polygon>(holes.count);
            foreach (var hole in holes)
            {
                if (toKeep.Any(o => o.Contains(hole, eps)))
                {
                    continue;
                }

                if (!contours.Any(o => o.ContainsOrIntersects(hole, eps)))
                {
                    continue;
                }

                toKeep.Add(hole);
            }
            return toKeep;
        }

        public Shape(List<Polygon> contours, List<Polygon>? holes = null, double eps = 1e-6)
        {
            if (contours.Count == 0)
            {
                throw new ArgumentException("Need at least one contour.");
            }

            List<Polygon> contoursToKeep = new List<Polygon>(contours.Count);
            
            _holes = holes is null ? new List<Polygon>() : ExtractValidHoles(contours, holes, eps);
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
