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
            var startNodes  = FindPortalNodes(portals, grid, start);
            HashSet<int> goalNodes  = new HashSet<int>(FindPortalNodes(portals, grid, end).Select(x => x.PortalKey));

            if (startNodes.Count == 0 || goalNodes.Count == 0)
                return null;
            
            HashSet<Cell> openSet = new HashSet<Cell>();
            HashSet<Cell> closedSet = new HashSet<Cell>();
            HashSet<Cell> startCells = new HashSet<Cell>();
            Cell endCell = grid[end.y, end.x];
            var cameFrom = new Dictionary<int, int>();
            
            foreach (var node in startNodes)
            {
                ref var portal = ref portals[node.PortalKey];
                var startCell = grid[portal.centerPos.y, portal.centerPos.x];
                startCell.PortalKey = node.PortalKey;
                openSet.Add(startCell);
                startCells.Add(startCell);
            }
            

            while (openSet.Count > 0)
            {
                Cell currentCell = Astar.GetNodeWithLowestFCost(openSet);
                if (goalNodes.Contains(currentCell.PortalKey))
                    return ReconstructPath(startCells, endCell);

                var currentPortal = portals[currentCell.PortalKey];
                openSet.Remove(currentCell);
                closedSet.Add(currentCell);

                //Check External Connection
                
                foreach (var portalKey in currentPortal.externalPortalConnections)
                {
                    if (portalKey == -1) break;
                    CheckConnection(grid, portals, portalKey, closedSet, currentCell, openSet, endCell, cameFrom);
                }
                
                //Check internal Connections
                foreach (var connection in currentPortal.internalPortalConnections)
                {
                    if(connection.portal == byte.MaxValue) continue; // portal is null
                    var portalKey =
                        Portal.GetPortalKeyFromInternalConnection(currentCell.PortalKey, connection.portal);
                    CheckConnection(grid, portals, portalKey, closedSet, currentCell, openSet, endCell, cameFrom);
                }
            }

            return null;
        }

        private static void CheckConnection(Cell[,] grid, Portal[] portals, int portalKey, HashSet<Cell> closedSet, Cell currentCell,
            HashSet<Cell> openSet, Cell endCell, Dictionary<int, int> cameFrom)
        {
            ref var portal = ref portals[portalKey];
            var neighbour = grid[portal.centerPos.y, portal.centerPos.x];
            if (closedSet.Contains(neighbour)) return;
            neighbour.PortalKey = portalKey;
            float tentativeGCost = currentCell.GCost + Astar.GetDistance(currentCell, neighbour);
            if (tentativeGCost < neighbour.GCost || !openSet.Contains(neighbour))
            {
                neighbour.GCost = tentativeGCost;
                neighbour.HCost = Astar.GetDistance(neighbour, endCell);
                neighbour.Parent = currentCell;

                if (!openSet.Contains(neighbour))
                {
                    cameFrom[currentCell.PortalKey] = portalKey;

                    openSet.Add(neighbour);
                }
            }
        }

        private static List<int> ReconstructPath(HashSet<Cell> startCells, Cell endCell)
        {
            List<int> path = new List<int>();
            Cell currentCell = endCell;

            while (!startCells.Contains(currentCell))
            {
                path.Add(currentCell.PortalKey);
                currentCell = currentCell.Parent;
            }
            ;
            path.Add(currentCell.PortalKey);

            path.Reverse();
            
            return path;
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