namespace HpaStarPathfinding.model.pathfinding
{
    public class PathfindingCell(Cell startCell)
    {
        public int FCost;
        
        public int GCost;
        public int HCost;
        
        public readonly Vector2D Position = startCell.Position;
        
        public PathfindingCell Parent = null!;
        
        public int QueueIndex;
        //all sides Connections
        public readonly byte Connections = startCell.Connections; 
        public int PortalKey;
    }
}