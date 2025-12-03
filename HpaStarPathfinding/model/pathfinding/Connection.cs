using System;

namespace HpaStarPathfinding.ViewModel
{
    public struct Connection
    {
        public byte portal;
        public ushort cost;

        public bool Equals(Connection other)
        {
            return portal == other.portal && cost == other.cost;
        }

        public override bool Equals(object obj)
        {
            return obj is Connection other && Equals(other);
        }
    }
}