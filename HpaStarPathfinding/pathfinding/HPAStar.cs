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
        
        public static List<PortalNode> FindPath(Cell[,] grid, Portal[] portals, Vector2D start, Vector2D end)
        {
            HashSet<PortalNode> openList = new HashSet<PortalNode>(FindPortalNodes(portals, grid, start));
            HashSet<PortalNode> goals = new HashSet<PortalNode>(FindPortalNodes(portals, grid, end));

            while (openList.Any())
            {
                var currentPortalNode = openList.First(); 
            }
            
            
            
            return null;
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

        public static List<Vector2D> PortalsToPath(Cell[,] grid, Portal[] portals, Vector2D pathStart, Vector2D pathEnd, List<PortalNode> pathAsPortals)
        {
            //Todo use cached paths
            List<Vector2D> path = Astar.FindPath(grid, pathStart, portals[pathAsPortals[0].PortalKey].centerPos);
            for (int i = 0; i < pathAsPortals.Count - 1; i++)
            {
                path.AddRange(Astar.FindPath(grid, portals[pathAsPortals[i].PortalKey].centerPos, portals[pathAsPortals[i + 1].PortalKey].centerPos));
            }

            path.AddRange(Astar.FindPath(grid, portals[pathAsPortals.Last().PortalKey].centerPos, pathEnd));
            return path;
        }

        
    }
}