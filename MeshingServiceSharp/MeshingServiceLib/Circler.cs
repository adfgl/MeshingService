namespace MeshingServiceLib
{
    public struct Circler(List<Triangle> triangles, int triangleIndex, int vertex)
    {
        readonly List<Triangle> _triangles = triangles;
        readonly int _startTriangle = triangleIndex, _vertex = vertex;
        int _current = triangleIndex;

        public readonly int Current => _current;
        public readonly int Vertex => _vertex;

        public bool Next()
        {
            Triangle curr = _triangles[_current];

            int next =
                curr.vtx0 == _vertex ? curr.adj0 :
                curr.vtx1 == _vertex ? curr.adj1 :
                curr.vtx2 == _vertex ? curr.adj2 :
                -1;

            if (next == _startTriangle || next == -1)
            {
                return false;
            }
            _current = next;
            return true;
        }
    }
}
