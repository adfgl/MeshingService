namespace TriUgla.Mesher
{
    public sealed class Shape
    {
        readonly List<Polygon> _holes;

     
        public Shape(Polygon contour, List<Polygon>? holes = null, double eps = 1e-6)
        {
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
                    if (contour.Contains(hole, eps) || contour.Intersects(hole))
                    {
                        bool discard = false;
                        foreach (Polygon holeKeep in holesKeep)
                        {
                            if (holeKeep.Contains(hole, eps))
                            {
                                discard = true;
                                break;
                            }
                        }

                        if (!discard)
                        {
                            holesKeep.Add(hole);
                        }
                    }
                }
            }

            Contour = contour;
            _holes = holesKeep;
            Eps = eps;  
        }

        public Polygon Contour { get; }
        public IReadOnlyList<Polygon> Holes => _holes;
        public double Eps { get; set; }


        public bool Contains(double x, double y) =>
                Contour.Contains(x, y, Eps) &&
                !Holes.Any(hole => hole.Contains(x, y, Eps));
    }
}
