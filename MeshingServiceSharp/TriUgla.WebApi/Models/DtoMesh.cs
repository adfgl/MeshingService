namespace TriUgla.WebApi.Models
{
    public sealed class DtoMesh
    {
        public List<DtoVertex> Vertices { get; set; }
        public List<DtoEdge> Edges { get; set; }
        public List<DtoFace> Faces { get; set; }
        public DtoMeshMeta Meta { get; set; }
    }
}