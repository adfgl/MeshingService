using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using static TriUgla.Mesher.MeshFinder;

namespace TriUgla.Mesher
{
    public static class MeshLegalization
    {
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        public static int Legalize(this Mesh mesh, ReadOnlySpan<int> indices, Stack<int>? affected = null)
        {
            Stack<int> stack = new Stack<int>(indices.Length);
            for (int i = 0; i < indices.Length; i++)
                stack.Push(indices[i]);
            return LegalizeCore(mesh, stack, affected);
        }

        public static int Legalize(this Mesh mesh, int[] indices, int count, Stack<int>? affected = null)
        {
            Stack<int> stack = new Stack<int>(count);
            for (int i = 0; i < count; i++)
                stack.Push(indices[i]);
            return LegalizeCore(mesh, stack, affected);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Key(int a, int b)
        {
            uint lo = (uint)(a < b ? a : b);
            uint hi = (uint)(a < b ? b : a);
            return ((ulong)hi << 32) | lo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Unpack(ulong key, out int a, out int b)
        {
            uint lo = (uint)(key & 0xFFFFFFFF);
            uint hi = (uint)(key >> 32);

            a = (int)lo;
            b = (int)hi;
        }

        public static int LegalizeCore(this Mesh mesh, Stack<int> stack, Stack<int>? affected)
        {
            int totalFlips = 0;
            Span<Triangle> tris = mesh.TrianglesSpan();

            Dictionary<ulong, int> flipCount = new Dictionary<ulong, int>(64);

            Span<int> newTriangles = stackalloc int[4];
            while (stack.Count != 0)
            {
                int ti = stack.Pop();
                affected?.Push(ti);

                ref Triangle t = ref tris[ti];
                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    int a, b;
                    if (edgeIndex == 0) { a = t.vtx0; b = t.vtx1; }
                    else if (edgeIndex == 1) { a = t.vtx1; b = t.vtx2; }
                    else { a = t.vtx2; b = t.vtx0; }

                    ulong key = Key(a, b);

                    flipCount.TryGetValue(key, out int flipsMade);
                    if (flipsMade >= MAX_FLIPS_PER_DIAGONAL)
                        continue;

                    if (!mesh.CanFlip(ti, edgeIndex, out bool should) || !should)
                        continue;

                    totalFlips++;
                    flipCount[key] = (byte)(flipsMade + 1);

                    int flippedCount = mesh.FlipCCW(newTriangles, ti, edgeIndex);
                    for (int i = 0; i < flippedCount; i++)
                    {
                        int idx = newTriangles[i];
                        stack.Push(idx);

                        if (affected is not null && ti != idx)
                            affected.Push(idx);
                    }
                    stack.Push(ti);
                    break;
                }
            }
            return totalFlips;
        }
    }
}
