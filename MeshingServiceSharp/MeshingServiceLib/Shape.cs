using System;
using System.Collections.Generic;
using System.Text;

namespace MeshingServiceLib
{
    public sealed class Shape(Polygon<Vertex> contour)
    {
        public Polygon<Vertex> Contour { get; set; } = contour;
        public List<Polygon<Vertex>>? Holes { get; set; }

        public bool Contains(double x, double y, double eps)
        {
            if (Contour.Contains(x, y, eps))
            {
                if (Holes is not null)
                {
                    foreach (Polygon<Vertex> hole in Holes)
                    {
                        if (hole.Contains(x, y, eps))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            return false;
        }
    }
}
