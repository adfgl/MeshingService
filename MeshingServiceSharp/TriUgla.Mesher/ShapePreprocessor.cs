public readonly struct Segment(Vertex start, Vertex end, string? id)
{
    public readonly Vertex start = start, end = end;
    public readonly string? id = id;

    public bool Contains(in Vertex vtx, double eps)
    {
        return 
            Vertex.Close(in start, in vtx, eps) ||
            Vertex.Close(in end, in vtx, eps);
    }

    public Vertex this[int index] => index == 0 ? start : end;
}

public sealed class ShapePreprocessor
{
    public void Do(Shape shape)
    {
        List<Vertex> vertices = new List<Vertex>();
    }

    public static void Process(Shape shape, List<Segment> conEdges, List<Vertex> conVertices)
    {
        var segments = new ();
        int nc = shape.Contours.Count;
        for (int i = 0; i < nc; i++)
        {
            var contour = shape.Contours[i];
            for (int j = 0; j < contour.Vertices.Count - 1; j++)
            {
                var seg = new Segment(contour.Vertices[j], contour.Vertices[j + 1]);
                var sub = Split(segments, in seg, eps);
                foreach (var item in sub)
                {
                    var ctr = Vertex.Between(in item.start, in item.end);
                    bool add = true;
                    for (int k = 0; k < nc; k++)
                    {
                        if (k == i) continue;

                        if (shape.Contours[k].Contains(ctr, eps) ||
                            shape.Holes.Any(o => o.Contains(ctr.x, ctr.y))
                        {
                            add = false;
                            break;
                        }
                    }
                    if (add) segments.Add(item);
                }  
            }
        }
    }

    public static double SqeLen(in Vertex a, in Vertex b)
    {
        double dx = a.x - b.x;
        double dy = a.y - b.y;
        return dx * dx + dy * dy;
    }

    public static void Split(List<Segment> segs, Vertex other, double eps)
    {
        int count = segs.Count;
        for (int i = 0; i < count; i++)
        {
            var seg = segs[i];
            var rect = Rectangle.From2Points(in seg.start, in seg.end);
            if (!rect.Contains(other.x, other.y) ||
                Vertex.Close(in seg.start, in other, eps) ||
                Vertex.Close(in seg.end, in other, eps))
            {
                continue;
            }

            if (GeometryHelper.OnSegment(in seg.start, in seg.end, in other))
            {
                Split(segs, i, in other);
                break;
            }
        }
    }

    public static List<Segment> Split(List<Segment> segs, Segment other, double eps)
    {
        var otherRect = Rectangle.From2Points(in other.start, in other.end);
        int count = segs.Count;
        
        List<Vertex> split = new ();
        for (int i = 0; i < count; i++)
        {
            Segment seg = segs[i];
            var rect = Rectangle.From2Points(in seg.start, in seg.end);
            if (!rect.Intersects(in other)) continue;
            
            bool containsStart = seg.Contains(other.start);
            bool containsEnd = seg.Contains(other.end);
            if (containsStart && containsEnd) return;
            if (containsStart || containsEnd) continue;
            
            bool shouldSplit = true;
            for (int j = 0; j < 2; j++)
            {
                Vertex v = j == 0 ? seg.start : seg.end;
                if (GeometryHelper.OnSegment(seg.start, seg.end, v))
                {
                    Split(segs, i, in v);
                    shouldSplit = false;
                    break;
                }
            }

            if (shouldSplit && 
                Intersect(seg.start, seg.end, other.start, other.end, out Vertex inter))
            {
                bool duplicate = false;
                foreach (var item in split)
                {
                    if (Vertex.Close(item, inter, eps))
                    {
                        duplicate = true;
                        break;
                    }
                }
                
                Split(segs, i, inter);
                
                if (!duplicate) split.Add(inter);
            }
        }

        var sorted = List<(double len, Vertex vtx)>(split.count + 2);
        split.Add(other.start);
        split.Add(other.end);
        foreach (var item in split)
            sorted.Add((SqrLen(item, other.start), item);

        sorted.Sort((a, b) => a.len.CompareTo(b.len));
        
        List<Segment> sub = new ();
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            sub.Add(sorted[i].vtx, sorted[i+1].vtx, other.id);
        }
        return sub;
    }

    public static void Split(List<Segment> segments, int index, in Vertex vtx)
    {
        var segment = segments[index];
        segments[index] = new (segment.start, vtx, segment.id);
        segments.Add(new (vtx, segment.end, segment.id));
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