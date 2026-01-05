public sealed class ShapePreprocessor
{
    public void Do(Shape shape)
    {
        List<Vertex> vertices = new List<Vertex>();
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