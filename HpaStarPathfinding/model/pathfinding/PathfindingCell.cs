namespace HpaStarPathfinding.ViewModel
{
    public class PathfindingCell
    {
        public int fCost;
        
        public int GCost;
        public int HCost;
        
        public readonly Vector2D Position;
        
        public PathfindingCell Parent;
        
        public int QueueIndex;
        //all sides Connections
        public byte Connections; 
        public int PortalKey;

        public PathfindingCell(Cell startCell)
        {
           Position = startCell.Position;   
           Connections = startCell.Connections;
        }
        
        
    }
}