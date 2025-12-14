namespace HpaStarPathfinding.model.pathfinding
{
    public struct Connection : IEquatable<Connection>
    {
        public byte portalKey;
        public ushort cost;

        public bool Equals(Connection other)
        {
            return portalKey == other.portalKey && cost == other.cost;
        }

        public override bool Equals(object? obj)
        {
            return obj is Connection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(portalKey, cost);
        }

        public static bool operator ==(Connection left, Connection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Connection left, Connection right)
        {
            return !(left == right);
        }
    }
}