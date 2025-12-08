using System;

namespace HpaStarPathfinding.ViewModel
{
    public struct Connection
    {
        public byte portalKey;
        public ushort cost;

        public bool Equals(Connection other)
        {
            return portalKey == other.portalKey && cost == other.cost;
        }

        public override bool Equals(object obj)
        {
            return obj is Connection other && Equals(other);
        }
    }
}