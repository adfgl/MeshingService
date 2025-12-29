using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TriUgla.Mesher
{
    public sealed class MeshLegalizer(Mesh mesh)
    {
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        readonly MeshProcessor _processor = new MeshProcessor(mesh);

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

        int LegalizeCore(MeshProcessor processor, Stack<int> stack, Stack<int>? affected)
        {
            int totalFlips = 0;
            Span<Triangle> tris = Triangles();

            Dictionary<ulong, int> flipCount = new Dictionary<ulong, int>(64);
            while (stack.Count > 0)
            {
                int ti = stack.Pop();
                affected?.Push(ti);

                ref Triangle t = ref tris[ti];
                int[] indices = [t.vtx0, t.vtx1, t.vtx2];
                for (int ei = 0; ei < 3; ei++)
                {
                    int u = indices[ei];
                    int v = indices[(ei + 1) % 3];
                    ulong key = MeshProcessor.Key(u, v);

                    flipCount.TryGetValue(key, out int flipsMade);
                    if (flipsMade >= MAX_FLIPS_PER_DIAGONAL)
                        continue;

                    if (!processor.CanFlip(ti, ei, out bool should) || !should)
                        continue;

                    totalFlips++;
                    flipCount[key] = (byte)(flipsMade + 1);

                    int flippedCount = processor.Flip(ti, ei, forceFlip: false);

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
