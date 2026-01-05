public sealed class ShapePreprocessor
{
    public void Do(Shape shape)
    {
        List<Vertex> vertices = new List<Vertex>();
    }

    public static void SplitEdge(List<ConstrainedEdge> edges, int edgeIndex, in Vertex vtx, double eps)
    {
        var edge = edges[edgeIndex];
        var cross = GeometryHelper.Cross(in edge.start, in edge.end, in vtx);
        if (Math.Abs(cross) > eps)
        {
            return;
        }

        if (Vertex.Close(in vtx, in edge.start, eps) ||
            Vertex.Close(in vtx, in edge.end, eps))
        {
            return;
        }

        if (GeometryHelper.InRectangle(
            edge.
    }

    public static int GetOrAdd(List<Vertex> existing, in Vertex vtx, double eps)
    {
        int count = existing.Count;
        for (int i = 0; i < count; i++)
        {
            var ex = existing[i];
            if (Vertex.Close(in ex, in vtx, eps))
            {
                string? id = GetId(ex.id, vtx.id);
                double z = Math.Max(ex.z, vtx.z);

                ex.id = id;
                ex.z = z;

                existing[i] = ex;
                return i;
            }
        }
        existing.Add(vtx);
        return count;
    }

    public static string? GetId(string? oldId, string? newId)
    {
        if (!String.IsNullOrEmpty(oldId)) 
            return newId;

        if (!String.IsNullOrEmpty(newId)) 
            return oldId;

        if (oldId.Length < newId.Length)
        {
            return newId
        }
        return oldId;
    }
}