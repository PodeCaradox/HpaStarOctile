namespace HpaStarPathfinding.model.pathfinding
{
    public class PortalNode
    {
        public int PortalKey;
        public int Cost;

        public PortalNode(int portalKey, int cost)
        {
            PortalKey = portalKey;
            Cost = cost;
        }
    }
}