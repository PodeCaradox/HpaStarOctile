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
        
        public static List<int> FindPath(Cell[,] grid, Chunk[,] chunks, Portal[] portals, Vector2D start, Vector2D end)
        {
            var startNodes  = FindPortalNodes(portals, grid, chunks, end);
            HashSet<int> goalNodes  = new HashSet<int>(FindPortalNodes(portals, grid, chunks, start).Select(x => x.PortalKey));
    
            if (startNodes.Count == 0 || goalNodes.Count == 0)
                return new List<int>();
            FastPriorityQueue open = new FastPriorityQueue(MainWindowViewModel.MaxPortalsInChunk * MainWindowViewModel.ChunkMapSize * MainWindowViewModel.ChunkMapSize);
            HashSet<int> closedSet = new HashSet<int>();
            Dictionary<int, PathfindingCell> getElement = new Dictionary<int, PathfindingCell>();
            PathfindingCell endCell = new PathfindingCell(grid[start.y, start.x]);
            
            foreach (var node in startNodes)
            {
                ref var portal = ref portals[node.PortalKey];
                var startCell = new PathfindingCell(grid[portal.CenterPos.y, portal.CenterPos.x]);
                startCell.PortalKey = node.PortalKey;
                startCell.GCost = node.Cost;
                startCell.HCost = Heuristic.GetHeuristic(startCell, endCell);
                open.Enqueue(startCell, startCell.GCost + startCell.HCost); //node.Cost + Astar.GetDistance(startCell, endCell)
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
    
                var g = currentCell.GCost;
                //Check external Connections
                foreach (var portalKey in currentPortal.ExternalPortalConnections)
                {
                    if (portalKey == -1) break;
                    CheckConnection(grid, portals, getElement, portalKey, closedSet, currentCell, open, endCell, g + StraightCost);
                }
                
                //Check internal Connections
                foreach (var connection in currentPortal.InternalPortalConnections)
                {
                    if(connection.portal == byte.MaxValue) break;
                    var portalKey =
                        Portal.GetPortalKeyFromInternalConnection(currentCell.PortalKey, connection.portal);
                    CheckConnection(grid, portals, getElement, portalKey, closedSet, currentCell, open, endCell, g + connection.cost);
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
            FastPriorityQueue open, PathfindingCell goalCell, float g)
        {
            
            if (getElement.TryGetValue(portalKey, out var neighbour)){}
            else
            {
                ref var portal = ref portals[portalKey];
                neighbour = new PathfindingCell(grid[portal.CenterPos.y, portal.CenterPos.x])
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
            else if (g + neighbour.HCost < neighbour.fCost) {
                neighbour.GCost = g;
                neighbour.Parent = currentCell;
                open.UpdatePriority(neighbour, neighbour.GCost + neighbour.HCost);
            }
        }
    
        private static List<PortalNode> FindPortalNodes(Portal[] portals, Cell[,] grid, Chunk[,] chunks, Vector2D goal)
        {                  
            Vector2D chunkPos = new Vector2D(goal.x / MainWindowViewModel.ChunkSize,
                goal.y / MainWindowViewModel.ChunkSize);      
            int chunkId = chunkPos.x + MainWindowViewModel.ChunkMapSize * chunkPos.y;

            int firstPortalPos = chunks[chunkPos.y, chunkPos.x].FirstPortalKey;
            
            Vector2D min = new Vector2D(chunkPos.x * MainWindowViewModel.ChunkSize, chunkPos.y * MainWindowViewModel.ChunkSize);
            Vector2D max = min + new Vector2D(MainWindowViewModel.ChunkSize, MainWindowViewModel.ChunkSize);
            
            int firstPossiblePortalKeyInChunk = Portal.GeneratePortalKey(chunkId, 0, 0);
            List<PortalNode> nodes = new List<PortalNode>();

            //Todo flood fill faster.
            int firstKey = firstPossiblePortalKeyInChunk + firstPortalPos;
            var firstPortal = portals[firstKey];
            float cost = AStarOnlyCost.FindPath(grid, firstPortal.CenterPos, goal, min, max);
            if(cost >= 0) nodes.Add(new PortalNode(firstKey, cost));
            
            for (int i = 0; i < firstPortal.InternalPortalConnections.Length; i++)
            {
                byte otherPortalKey = firstPortal.InternalPortalConnections[i].portal;
                if(otherPortalKey == byte.MaxValue) break;
                cost = AStarOnlyCost.FindPath(grid, portals[firstPossiblePortalKeyInChunk + otherPortalKey].CenterPos, goal, min, max);
                if(cost < 0) continue;
                nodes.Add(new PortalNode(firstPossiblePortalKeyInChunk + otherPortalKey, cost));
            }
            return nodes;
        }
    
        public static List<Vector2D> PortalsToPath(Cell[,] grid, Portal[] portals, Vector2D pathStart, Vector2D pathEnd, List<int> pathAsPortals)
        {
            //Todo use cached paths
            List<Vector2D> path = Astar.FindPath(grid, pathStart, portals[pathAsPortals[0]].CenterPos);
            for (int i = 0; i < pathAsPortals.Count - 1; i++)
            {
                path.AddRange(Astar.FindPath(grid, portals[pathAsPortals[i]].CenterPos, portals[pathAsPortals[i + 1]].CenterPos));
            }
    
            path.AddRange(Astar.FindPath(grid, portals[pathAsPortals.Last()].CenterPos, pathEnd));
            return path;
        }
    
        
    }
}