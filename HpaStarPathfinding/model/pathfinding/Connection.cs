using System;

namespace HpaStarPathfinding.ViewModel
{
    public struct Connection : IEquatable<Connection>
    {
        public byte portal;
        public float cost;

        public bool Equals(Connection other)
        {
            return portal == other.portal && cost == other.cost;
        }

        public override bool Equals(object obj)
        {
            return obj is Connection other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (portal.GetHashCode() * 397) ^ cost.GetHashCode();
            }
        }
    }
}