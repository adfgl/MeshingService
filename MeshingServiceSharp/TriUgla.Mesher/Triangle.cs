using System.Runtime.CompilerServices;

namespace TriUgla.Mesher
{
    public struct Triangle(
        int vtx0, int vtx1, int vtx2,
        int adj0 = -1, int adj1 = -1, int adj2 = -1,
        int con0 = -1, int con1 = -1, int con2 = -1)
    {
        public int vtx0 = vtx0, vtx1 = vtx1, vtx2 = vtx2;
        public int adj0 = adj0, adj1 = adj1, adj2 = adj2;
        public int con0 = con0, con1 = con1, con2 = con2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(int v) =>
          v == vtx0 ? 0 : v == vtx1 ? 1 : v == vtx2 ? 2 : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(int start, int end) =>
            start == vtx0 ? (end == vtx1 ? 0 : -1) :
            start == vtx1 ? (end == vtx2 ? 1 : -1) :
            start == vtx2 ? (end == vtx0 ? 2 : -1) : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOfInvariant(int start, int end) =>
            start == vtx0 ? (end == vtx1 ? 0 : end == vtx2 ? 2 : -1) :
            start == vtx1 ? (end == vtx2 ? 1 : end == vtx0 ? 0 : -1) :
            start == vtx2 ? (end == vtx0 ? 2 : end == vtx1 ? 1 : -1) :
            -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Triangle Orient(int edge)
        {
            if (edge == 0) return this;
            if (edge == 1)
            {
                return new Triangle(
                       vtx1, vtx2, vtx0,
                       adj1, adj2, adj0,
                       con1, con2, con0);
            }
            return new Triangle(
                      vtx2, vtx0, vtx1,
                      adj2, adj0, adj1,
                      con2, con0, con1);
        }
    }
}
