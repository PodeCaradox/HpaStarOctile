using HpaStarPathfinding.model.pathfinding;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public static class HpaStar
    {
        
        public static List<int> FindPath(Cell[] grid, Portal?[] portals, Vector2D start, Vector2D end)
        {
            byte regionPortalEnd = grid[end.y * MapSizeX + end.x].Region;
            if(regionPortalEnd == byte.MaxValue) return [];
            byte regionPortalStart = grid[start.y * MapSizeX + start.x].Region;
            if(regionPortalStart == byte.MaxValue) return [];
            
            var startNodes  = FindPortalNodes(portals, grid, end, regionPortalEnd);
            HashSet<int> goalNodes  = new HashSet<int>(FindPortalNodes(portals, grid, start, regionPortalStart).Select(x => x.PortalKey));
            //TODO maybe calc how many portals are currently on the Map
            FastPriorityQueue open = new FastPriorityQueue(MaxPortalsInChunk * ChunkMapSizeX * ChunkMapSizeY);
            HashSet<int> closedSet = [];
            Dictionary<int, PathfindingCell> getElement = new Dictionary<int, PathfindingCell>();
            PathfindingCell endCell = new PathfindingCell(grid[start.y * MapSizeX + start.x]);
            
            foreach (var node in startNodes)
            {
                ref var portal = ref portals[node.PortalKey]!;
                var startCell = new PathfindingCell(grid[portal.CenterPos.y * MapSizeX + portal.CenterPos.x])
                {
                    PortalKey = node.PortalKey,
                    GCost = node.Cost
                };
                startCell.HCost = Heuristic.GetHeuristic(startCell, endCell);
                open.Enqueue(startCell, startCell.GCost + startCell.HCost);
                getElement.Add(startCell.PortalKey, startCell);
            }

            bool finished = false;
            PathfindingCell? currentCell = null;
            while (open.Count > 0)
            {
                currentCell = open.Dequeue();
                if (goalNodes.Contains(currentCell.PortalKey))
                {
                    finished = true;
                    break;
                }
    
                var currentPortal = portals[currentCell.PortalKey]!;
                closedSet.Add(currentCell.PortalKey);
    
                var g = currentCell.GCost;
                //Check external Connections
                int extLength = currentPortal.ExtIntPortalCount >> (int)ExternalInternalLength.OffsetExtLength;
                for (int i = 0; i < extLength; i++)
                {
                    CheckConnection(grid, portals, getElement, currentPortal.ExternalPortalConnections[i], closedSet, currentCell, open, endCell, g + Heuristic.StraightCost);
                }
                
                //Check internal Connections
                int intLength = currentPortal.ExtIntPortalCount & (int)ExternalInternalLength.InternalLength;
                for (int i = 0; i < intLength; i++)
                {
                    ref var connection = ref currentPortal.InternalPortalConnections[i];
                    var portalKey = Portal.GetPortalKeyFromInternalConnection(currentCell.PortalKey, connection.portalKey);
                    CheckConnection(grid, portals, getElement, portalKey, closedSet, currentCell, open, endCell, g + connection.cost);
                }
            }
    
            
            if(!finished) return [];
            
            var path = new List<int>();
            while (currentCell != null) {
                path.Add(currentCell.PortalKey);
                currentCell = currentCell.Parent;
            }

            return path;
        }
    
        private static void CheckConnection(Cell[] grid, Portal?[] portals, Dictionary<int, PathfindingCell> getElement, int portalKey, HashSet<int> closedSet, PathfindingCell currentCell,
            FastPriorityQueue open, PathfindingCell goalCell, int g)
        {
            
            if (getElement.TryGetValue(portalKey, out var neighbour)){}
            else
            {
                ref var portal = ref portals[portalKey]!;
                neighbour = new PathfindingCell(grid[portal.CenterPos.y * MapSizeX + portal.CenterPos.x])
                {
                    PortalKey = portalKey
                };
                getElement.Add(portalKey, neighbour);
            }
            if (closedSet.Contains(portalKey)) return;
            
            neighbour.PortalKey = portalKey;
            
            if (!open.Contains(neighbour))
            {
                neighbour.GCost = g;
                neighbour.HCost = Heuristic.GetHeuristic(neighbour, goalCell);
                neighbour.Parent = currentCell;
                open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
            } 
            else if (g + neighbour.HCost < neighbour.FCost) {
                neighbour.GCost = g;
                neighbour.Parent = currentCell;
                open.UpdatePriority(neighbour, neighbour.GCost + neighbour.HCost);
            }
        }
    
        private static List<PortalNode> FindPortalNodes(Portal?[] portals, Cell[] grid, Vector2D start, byte region)
        {              
            int chunkId = start.x / ChunkSize + ChunkMapSizeX * (start.y / ChunkSize);
            ushort[] costFields = BFS.BfsFromStartPos(grid, start);
            int firstPossiblePortal = Portal.GeneratePortalKey(chunkId, 0, 0);
            
            List<PortalNode> nodes = new List<PortalNode>();
            var portal = AddPortal(portals, firstPossiblePortal, region, costFields, nodes);
            int length = portal.ExtIntPortalCount & (int)ExternalInternalLength.InternalLength;
            for (int j = 0; j < length; j++)
            {
                AddPortal(portals, firstPossiblePortal, portal.InternalPortalConnections[j].portalKey, costFields, nodes);
            }
            
            return nodes;
        }

        private static Portal AddPortal(Portal?[] portals, int firstPossiblePortal, byte portalKey,
            ushort[] costFields, List<PortalNode> nodes)
        {
            ref var portal = ref portals[firstPossiblePortal + portalKey]!;
            var cost = BFS.GetCostForPath(costFields, portal.CenterPos);
            nodes.Add(new PortalNode(firstPossiblePortal + portalKey, cost));
            return portal;
        }

        public static List<Vector2D> PortalsToPath(Cell[] grid, Portal?[] portals, Vector2D pathStart, Vector2D pathEnd, List<int> pathAsPortals)
        {
            List<Vector2D> path = Astar.FindPath(grid, pathStart, portals[pathAsPortals[0]]!.CenterPos);
            for (int i = 0; i < pathAsPortals.Count - 1; i++)
            {
                path.AddRange(Astar.FindPath(grid, portals[pathAsPortals[i]]!.CenterPos, portals[pathAsPortals[i + 1]]!.CenterPos));
            }
    
            path.AddRange(Astar.FindPath(grid, portals[pathAsPortals.Last()]!.CenterPos, pathEnd));
            return path;
        }
    
        
    }
}