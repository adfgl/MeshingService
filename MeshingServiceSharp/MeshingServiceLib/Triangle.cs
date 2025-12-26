using System.Runtime.CompilerServices;

namespace MeshingServiceLib
{
    public struct Triangle(
        int vtx0, int vtx1, int vtx2,
        int adj0, int adj1, int adj2,
        int con0, int con1, int con2)
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
        public readonly void Edge(int i, out int start, out int end)
        {
            if (i < 0)
            {
                start = end = -1;
                return;
            }

#if DEBUG
            if (i > 2)
            {
                start = end = -1;
                return;
            }
#endif

            switch (i)
            {
                case 0: start = vtx0; end = vtx1; return;
                case 1: start = vtx1; end = vtx2; return;
                default: start = vtx2; end = vtx0; return; 
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Triangle Orient(int edge)
        {
            if (edge < 0) throw new Exception();
#if DEBUG
            if (edge > 2) throw new Exception();
#endif
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Triangle Orient(int start, int end)
        {
            if (start == vtx0)
                return end == vtx1 ? this : throw new Exception();

            if (start == vtx1)
            {
                if (end != vtx2) throw new Exception();
                return new Triangle(
                    vtx1, vtx2, vtx0,
                    adj1, adj2, adj0,
                    con1, con2, con0);
            }

            if (start == vtx2)
            {
                if (end != vtx0) throw new Exception();
                return new Triangle(
                    vtx2, vtx0, vtx1,
                    adj2, adj0, adj1,
                    con2, con0, con1);
            }
            throw new Exception();
        }
    }
}
