namespace TriUgla.WebApi.Services
{
    public sealed record MeshingResult(
        string JobId,
        string MeshFormat,
        string MeshPayload);
}
