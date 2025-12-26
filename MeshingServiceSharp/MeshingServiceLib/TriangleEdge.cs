using System;
using System.Runtime.CompilerServices;

namespace MeshingServiceLib
{
    public struct TriangleEdge(string? id, int start, int end, int triangle)
    {
        public readonly string? id = id;
        public readonly int start = start, end = end;
        public readonly ulong key = EdgeKey(start, end);
        public int triangle = triangle;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong EdgeKey(int a, int b)
        {
            uint lo = (uint)(a < b ? a : b);
            uint hi = (uint)(a < b ? b : a);
            return ((ulong)hi << 32) | lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnpackEdgeKey(ulong key, out int a, out int b)
        {
            uint lo = (uint)(key & 0xFFFFFFFF);
            uint hi = (uint)(key >> 32);

            a = (int)lo;
            b = (int)hi;
        }


     
    }
}
