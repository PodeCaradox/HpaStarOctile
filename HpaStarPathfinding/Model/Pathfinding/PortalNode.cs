namespace HpaStarPathfinding.ViewModel
{
    public class PortalNode
    {
        public int PortalKey;
        public float Cost;

        public PortalNode(int portalKey, float cost)
        {
            this.PortalKey = portalKey;
            this.Cost = cost;
        }
    }
}