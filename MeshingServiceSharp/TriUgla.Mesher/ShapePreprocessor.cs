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
            if (Vertex.Close(in existing[i], in vtx, eps))
            {
                return i;
            }
        }
        existing.Add(vtx);
        return count;
    }
}