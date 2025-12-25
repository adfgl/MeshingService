namespace MeshingServiceLib
{
    public sealed class TriangleLegalizer(Mesh mesh)
    {
        public const int MAX_FLIPS_PER_DIAGONAL = 5;

        public Mesh Mesh => mesh;

        public int Legalize(IEnumerable<int> indices, Stack<int>? affected = null)
        {
            TriangleFlipper flipper = new TriangleFlipper(Mesh);

            Stack<int> stack = new Stack<int>(indices);

            int totalFlips = 0;
            List<Triangle> tris = Mesh.Triangles;
            Dictionary<Edge, int> flipCount = new Dictionary<Edge, int>(64);
            while (stack.Count > 0)
            {
                int ti = stack.Pop();
                affected?.Push(ti);

                Triangle t = tris[ti];
                for (int ei = 0; ei < 3; ei++)
                {
                    t.Edge(ei, out int u, out int v);
                    Edge key = new Edge(u, v);

                    if (flipCount.TryGetValue(key, out int flipsMade) && flipsMade >= MAX_FLIPS_PER_DIAGONAL)
                    {
                        continue;
                    }

                    if (!flipper.CanFlip(ti, ei, out bool should) || !should)
                    {
                        continue;
                    }

                    totalFlips++;
                    flipCount[key] = flipsMade + 1;

                    for (int i = 0; i < flipper.Flip(ti, ei, false); i++)
                    {
                        int idx = flipper.NewTriangles[i];
                        stack.Push(idx);

                        if (ti != idx && affected is not null)
                        {
                            affected.Push(idx);
                        }
                    }

                    stack.Push(ti);
                    break;

                }
            }
            return totalFlips;
        }

        readonly struct Edge : IEquatable<Edge>
        {
            public readonly int start, end;

            public Edge(int start, int end)
            {
                if (start < end)
                {
                    this.start = start;
                    this.end = end;
                }
                else
                {
                    this.start = end;
                    this.end = start;
                }
            }

            public override int GetHashCode() => HashCode.Combine(start, end);

            public bool Equals(Edge other)
            {
                return start == other.start && end == other.end;
            }

            public override bool Equals(object? obj)
            {
                return obj is Edge other && Equals(other);
            }
        }
    }
}
