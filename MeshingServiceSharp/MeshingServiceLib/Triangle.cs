using System.Runtime.CompilerServices;

namespace MeshingServiceLib
{
    public struct Triangle(
        int vtx0, int vtx1, int vtx2,
        int adj0, int adj1, int adj2,
        bool con0, bool con1, bool con2,
        TriangleState state)
    {
        public static readonly Triangle Dead = new Triangle(-1, -1, -1, -1, -1, -1, false, false, false, TriangleState.Ambiguous);

        public int vtx0 = vtx0, vtx1 = vtx1, vtx2 = vtx2;
        public int adj0 = adj0, adj1 = adj1, adj2 = adj2;
        public bool con0 = con0, con1 = con1, con2 = con2;
        public TriangleState state = state;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(int vertex)
        {
            if (vertex == vtx0) return 0;
            if (vertex == vtx1) return 1;
            return vertex == vtx2 ? 2 : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOf(int start, int end)
        {
            if (start == vtx0) return end == vtx1 ? 0 : -1;
            if (start == vtx1) return end == vtx2 ? 1 : -1;
            if (start == vtx2) return end == vtx0 ? 2 : -1;
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOfInvariant(int start, int end)
        {
            int edge = IndexOf(start, end);
            if (edge == -1) edge = IndexOf(end, start);
            return edge;
        }

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
            if (edge < 0) return Dead;
#if DEBUG
            if (edge > 2) return Dead;
#endif

            switch (edge)
            {
                case 0:
                    return this;

                case 1:
                    return new Triangle(
                        vtx1, vtx2, vtx0,
                        adj1, adj2, adj0,
                        con1, con2, con0,
                        state);

                default: 
                    return new Triangle(
                        vtx2, vtx0, vtx1,
                        adj2, adj0, adj1,
                        con2, con0, con1,
                        state);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Triangle Orient(int start, int end)
        {
            if (start == vtx0)
                return end == vtx1 ? this : Dead;

            if (start == vtx1)
            {
                if (end != vtx2) return Dead;
                return new Triangle(
                    vtx1, vtx2, vtx0,
                    adj1, adj2, adj0,
                    con1, con2, con0,
                    state);
            }

            if (start == vtx2)
            {
                if (end != vtx0) return Dead;
                return new Triangle(
                    vtx2, vtx0, vtx1,
                    adj2, adj0, adj1,
                    con2, con0, con1,
                    state);
            }
            return Dead;
        }
    }
}
