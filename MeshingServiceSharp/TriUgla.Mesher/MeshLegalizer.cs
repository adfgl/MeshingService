using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class MeshLegalizer(Mesh mesh, MeshProcessor? processor = null)
    {
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        readonly MeshProcessor _processor = processor ?? new MeshProcessor(mesh);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Span<Triangle> Triangles() => CollectionsMarshal.AsSpan(mesh.Triangles);

        public int Legalize(ReadOnlySpan<int> indices, Stack<int>? affected = null)
        {
            Stack<int> stack = new Stack<int>(indices.Length);
            for (int i = 0; i < indices.Length; i++)
                stack.Push(indices[i]);
            return LegalizeCore(_processor, stack, affected);
        }

        public int Legalize(int[] indices, int count, Stack<int>? affected = null)
        {
            Stack<int> stack = new Stack<int>(count);
            for (int i = 0; i < count; i++)
                stack.Push(indices[i]);
            return LegalizeCore(_processor, stack, affected);
        }

        readonly struct Edge(int start, int end)
        {
            public readonly int start = start, end = end;
        }

        int LegalizeCore(MeshProcessor processor, Stack<int> stack, Stack<int>? affected)
        {
            int totalFlips = 0;
            Span<Triangle> tris = Triangles();

            Dictionary<ulong, int> flipCount = new Dictionary<ulong, int>(64);

            Edge[] edges = new Edge[3];
            while (stack.Count > 0)
            {
                int ti = stack.Pop();
                affected?.Push(ti);

                ref Triangle t = ref tris[ti];
                edges[0] = new Edge(t.vtx0, t.vtx1);
                edges[1] = new Edge(t.vtx1, t.vtx2);
                edges[2] = new Edge(t.vtx2, t.vtx0);

                for (int edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    Edge edge = edges[edgeIndex];
                    ulong key = MeshProcessor.Key(edge.start, edge.end);

                    flipCount.TryGetValue(key, out int flipsMade);
                    if (flipsMade >= MAX_FLIPS_PER_DIAGONAL)
                        continue;

                    if (!processor.CanFlip(ti, edgeIndex, out bool should) || !should)
                        continue;

                    totalFlips++;
                    flipCount[key] = (byte)(flipsMade + 1);

                    int flippedCount = processor.FlipCCW(ti, edgeIndex);
                    for (int i = 0; i < flippedCount; i++)
                    {
                        int idx = processor.New[i];
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
