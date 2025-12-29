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
        public static int IndexOf(in Triangle t, int v) =>
            v == t.vtx0 ? 0 : v == t.vtx1 ? 1 : v == t.vtx2 ? 2 : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(in Triangle t, int start, int end) =>
            start == t.vtx0 ? (end == t.vtx1 ? 0 : -1) :
            start == t.vtx1 ? (end == t.vtx2 ? 1 : -1) :
            start == t.vtx2 ? (end == t.vtx0 ? 2 : -1) : -1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfInvariant(in Triangle t, int start, int end) =>
            start == t.vtx0 ? (end == t.vtx1 ? 0 : end == t.vtx2 ? 2 : -1) :
            start == t.vtx1 ? (end == t.vtx2 ? 1 : end == t.vtx0 ? 0 : -1) :
            start == t.vtx2 ? (end == t.vtx0 ? 2 : end == t.vtx1 ? 1 : -1) :
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
