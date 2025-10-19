using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.ViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class HPAStar
    {
        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.414f;
        
        public static List<int> FindPath(Cell[,] grid, Portal[] portals, Vector2D start, Vector2D end)
        {
            var startNodes  = FindPortalNodes(portals, grid, end);
            HashSet<int> goalNodes  = new HashSet<int>(FindPortalNodes(portals, grid, start).Select(x => x.PortalKey));
    
            if (startNodes.Count == 0 || goalNodes.Count == 0)
                return new List<int>();
            FastPriorityQueue open = new FastPriorityQueue(MainWindowViewModel.MaxPortalsInChunk * MainWindowViewModel.ChunkMapSize * MainWindowViewModel.ChunkMapSize);
            HashSet<int> closedSet = new HashSet<int>();
            Dictionary<int, PathfindingCell> getElement = new Dictionary<int, PathfindingCell>();
            PathfindingCell endCell = new PathfindingCell(grid[end.y, end.x]);
            
            foreach (var node in startNodes)
            {
                ref var portal = ref portals[node.PortalKey];
                var startCell = new PathfindingCell(grid[portal.centerPos.y, portal.centerPos.x]);
                startCell.PortalKey = node.PortalKey;
                open.Enqueue(startCell, 0);
                getElement.Add(startCell.PortalKey, startCell);
            }
            
            PathfindingCell currentCell = null;
            bool finished = false;
            while (open.Count > 0)
            {
                currentCell = open.Dequeue();
                if (goalNodes.Contains(currentCell.PortalKey))
                {
                    finished = true;
                    break;
                }
    
                var currentPortal = portals[currentCell.PortalKey];
                closedSet.Add(currentCell.PortalKey);
    
                var g = currentCell.GCost + 1;
                //Check external Connections
                foreach (var portalKey in currentPortal.externalPortalConnections)
                {
                    if (portalKey == -1) break;
                    CheckConnection(grid, portals, getElement, portalKey, closedSet, currentCell, open, endCell, g);
                }
                
                //Check internal Connections
                foreach (var connection in currentPortal.internalPortalConnections)
                {
                    if(connection.portal == byte.MaxValue) break;
                    var portalKey =
                        Portal.GetPortalKeyFromInternalConnection(currentCell.PortalKey, connection.portal);
                    CheckConnection(grid, portals, getElement, portalKey, closedSet, currentCell, open, endCell, g);
                }
            }
    
            var path = new List<int>();
            if(!finished) return path;
            
            while (currentCell != null) {
                path.Add(currentCell.PortalKey);
                currentCell = currentCell.Parent;
            }

            return path;
        }
    
        private static void CheckConnection(Cell[,] grid, Portal[] portals, Dictionary<int, PathfindingCell> getElement, int portalKey, HashSet<int> closedSet, PathfindingCell currentCell,
            FastPriorityQueue open, PathfindingCell endCell, float g)
        {
            
            if (getElement.TryGetValue(portalKey, out var neighbour)){}
            else
            {
                ref var portal = ref portals[portalKey];
                neighbour = new PathfindingCell(grid[portal.centerPos.y, portal.centerPos.x])
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
                neighbour.HCost = GetDistance(neighbour, endCell);
                neighbour.Parent = currentCell;
                open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
            }
        }
        
        public static float GetDistance(PathfindingCell a, PathfindingCell b)
        {
            int dX = Math.Abs(a.Position.x - b.Position.x);
            int dY = Math.Abs(a.Position.y - b.Position.y);
            return StraightCost * (dX + dY) + DiagonalCost * Math.Min(dX, dY);
        }
    
        private static List<PortalNode> FindPortalNodes(Portal[] portals, Cell[,] grid, Vector2D goal)
        {
            Vector2D chunkPos = new Vector2D(goal.x / MainWindowViewModel.ChunkSize,
                goal.y / MainWindowViewModel.ChunkSize);
            int chunkId = chunkPos.x + MainWindowViewModel.ChunkMapSize * chunkPos.y;
            //get chunk, get all Portals in Chunk, calculate Paths from end node to portals
            int firstPortalKey = Portal.GeneratePortalKey(chunkId, 0, 0);
            List<PortalNode> nodes = new List<PortalNode>();
            Vector2D min = new Vector2D(chunkPos.x * MainWindowViewModel.ChunkSize, chunkPos.y * MainWindowViewModel.ChunkSize);
            Vector2D max = min + new Vector2D(MainWindowViewModel.ChunkSize, MainWindowViewModel.ChunkSize);
            for (int i = 0; i < MainWindowViewModel.MaxPortalsInChunk; i++)
            {
                int currentPortal = firstPortalKey + i;
                if (portals[currentPortal] == null) continue;
                //Todo flood fill could be faster.
                float cost = AStarCustom.FindPath(grid, portals[currentPortal].centerPos, goal, min, max);
                if(cost < 0) continue;
                nodes.Add(new PortalNode(currentPortal, cost));
            }
            return nodes;
        }
    
        public static List<Vector2D> PortalsToPath(Cell[,] grid, Portal[] portals, Vector2D pathStart, Vector2D pathEnd, List<int> pathAsPortals)
        {
            //Todo use cached paths
            List<Vector2D> path = Astar.FindPath(grid, pathStart, portals[pathAsPortals[0]].centerPos);
            for (int i = 0; i < pathAsPortals.Count - 1; i++)
            {
                path.AddRange(Astar.FindPath(grid, portals[pathAsPortals[i]].centerPos, portals[pathAsPortals[i + 1]].centerPos));
            }
    
            path.AddRange(Astar.FindPath(grid, portals[pathAsPortals.Last()].centerPos, pathEnd));
            return path;
        }
    
        
    }
}