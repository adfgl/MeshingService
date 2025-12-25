namespace MeshingServiceLib
{
    public interface ISuperStructure
    {
        int SuperVertices { get; }
        Mesh Build(Polygon<Vertex> polygon, double scale);
    }


}
