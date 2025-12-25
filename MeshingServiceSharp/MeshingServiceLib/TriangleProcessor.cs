using Microsoft.VisualBasic;
using System.Xml.Linq;

namespace MeshingServiceLib
{
    public class TriangleProcessor(Mesh mesh)
    {
        readonly int[] _newTriangles = new int[4];

        public Mesh Mesh => mesh;
        public int[] NewTriangles => _newTriangles;

        public void Split(int edge, int vertex, out TriangleEdge a, out TriangleEdge b)
        {
            TriangleEdge e = Mesh.Edges[edge];
            a = new TriangleEdge(e.id, e.start, vertex, -1);
            b = new TriangleEdge(e.id, vertex, e.end, -1);
        }

        public void SetAdjacent(int adjIndex, int adjStart, int adjEnd, int value)
        {
            if (adjIndex == -1) return;

            Triangle t = Mesh.Triangles[adjIndex];
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

            Mesh.Triangles[adjIndex] = t;
        }

    }
}
