using System.Runtime.CompilerServices;

namespace TriUgla.Mesher
{
    public static class MeshHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetAdjacent(Span<Triangle> tris, int adjIndex, int adjStart, int adjEnd, int value)
        {
            if (adjIndex < 0) return;

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
    }
}
