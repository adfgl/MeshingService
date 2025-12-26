namespace TriUgla.Mesher
{
    public readonly struct Rectangle(double minX, double minY, double maxX, double maxY)
    {
        public readonly double minX = minX, minY = minY;
        public readonly double maxX = maxX, maxY = maxY;

        public static Rectangle Empty => new Rectangle(double.MaxValue, double.MaxValue, double.MinValue, double.MinValue);

        public static Rectangle From2Points(in Vertex a, in Vertex b)
        {
            return new Rectangle(
                a.x < b.x ? a.x : b.x,
                a.y < b.y ? a.y : b.y,
                a.x > b.x ? a.x : b.x,
                a.y > b.y ? a.y : b.y);
        }

        public static Rectangle FromCircle(in Circle circle)
        {
            double radius = Math.Sqrt(circle.radiusSqr);
            double x = circle.x;
            double y = circle.y;
            return new Rectangle(x - radius, y - radius, x + radius, y + radius);
        }

        public double Width() => this.maxX - this.minX;
        public double Height() => this.maxY - this.minY;

        public static Rectangle FromPoints<T>(IEnumerable<T> points, Func<T, double> getX, Func<T, double> getY)
        {
            double minX, minY, maxX, maxY;
            minX = minY = double.MaxValue;
            maxX = maxY = double.MinValue;
            foreach (T point in points)
            {
                double x = getX(point);
                double y = getY(point);

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
            return new Rectangle(minX, minY, maxX, maxY);
        }

        public Rectangle Expand(double margin)
        {
            return new Rectangle(
                minX - margin,
                minY - margin,
                maxX + margin,
                maxY + margin
            );
        }

        public bool IntersectsCircle(double cx, double cy, double radius)
        {
            double closestX = Math.Max(minX, Math.Min(cx, maxX));
            double closestY = Math.Max(minY, Math.Min(cy, maxY));

            double dx = cx - closestX;
            double dy = cy - closestY;
            return dx * dx + dy * dy <= radius * radius;
        }

        public Rectangle Union(double x, double y)
        {
            return new Rectangle(
                Math.Min(minX, x), Math.Min(minY, y),
                Math.Max(maxX, x), Math.Max(maxY, y)
            );
        }

        public Rectangle Union(Rectangle other)
        {
            return new Rectangle(
                Math.Min(minX, other.minX), Math.Min(minY, other.minY),
                Math.Max(maxX, other.maxX), Math.Max(maxY, other.maxY)
            );
        }

        public bool Intersection(Rectangle other, out Rectangle intersection)
        {
            double minX = Math.Max(this.minX, other.minX);
            double minY = Math.Max(this.minY, other.minY);
            double maxX = Math.Min(this.maxX, other.maxX);
            double maxY = Math.Min(this.maxY, other.maxY);
            if (minX <= maxX && minY <= maxY)
            {
                intersection = new Rectangle(minX, minY, maxX, maxY);
                return true;
            }

            intersection = Empty;
            return false;
        }

        public Rectangle Move(double dx, double dy) => new Rectangle(minX + dx, minY + dy, maxX + dx, maxY + dy);

        public bool Contains(double x, double y) => x >= minX && x <= maxX && y >= minY && y <= maxY;
        public bool ContainsStrict(double x, double y) => x > minX && x < maxX && y > minY && y < maxY;

        public bool Contains(Rectangle other) =>
            minX <= other.minX && minY <= other.minY &&
            maxX >= other.maxX && maxY >= other.maxY;

        public bool ContainsStrict(Rectangle other) =>
            minX < other.minX && minY < other.minY &&
            maxX > other.maxX && maxY > other.maxY;

        public bool Intersects(Rectangle other) =>
            minX <= other.maxX && minY <= other.maxY &&
            maxX >= other.minX && maxY >= other.minY;

        public bool IntersectsStrict(Rectangle other) =>
            minX < other.maxX && minY < other.maxY &&
            maxX > other.minX && maxY > other.minY;
    }
}
