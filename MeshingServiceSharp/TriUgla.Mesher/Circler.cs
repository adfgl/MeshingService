namespace TriUgla.Mesher
{
    public ref struct Circler(ReadOnlySpan<Triangle> tris, int triangleIndex, int vertexIndex)
    {
        readonly ReadOnlySpan<Triangle> _tris = tris;
        readonly int _startTriangle = triangleIndex, _vertex = vertexIndex;
        int _current = triangleIndex;

        public readonly int CurrentTriangle => _current;
        public readonly int TriangleIndex => _startTriangle;
        public readonly int VertexIndex => _vertex;

        public bool Next()
        {
            ref readonly Triangle curr = ref _tris[_current];

            int next =
                curr.vtx0 == _vertex ? curr.adj0 :
                curr.vtx1 == _vertex ? curr.adj1 :
                curr.vtx2 == _vertex ? curr.adj2 :
                -1;

            if (next == -1 || next == _startTriangle) return false;
            _current = next;
            return true;
        }
    }
}
