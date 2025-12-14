using System;
using System.Collections.Generic;
using System.Linq;
using HpaStarPathfinding.model.Pathfinding;
using HpaStarPathfinding.ViewModel;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding
{
    public class HPAStar
    {
        
        public static List<int> FindPath(Cell[] grid, Chunk[] chunks, Portal[] portals, Vector2D start, Vector2D end)
        {
            var startNodes  = FindPortalNodes(portals, grid, chunks, end);
            if (startNodes.Count == 0) return new List<int>();
            
            HashSet<int> goalNodes  = new HashSet<int>(FindPortalNodes(portals, grid, chunks, start).Select(x => x.PortalKey));
            if (goalNodes.Count == 0) return new List<int>();
            
            FastPriorityQueue open = new FastPriorityQueue(MaxPortalsInChunk * ChunkMapSizeX * ChunkMapSizeX);
            HashSet<int> closedSet = new HashSet<int>();
            Dictionary<int, PathfindingCell> getElement = new Dictionary<int, PathfindingCell>();
            PathfindingCell endCell = new PathfindingCell(grid[start.y * MapSizeX + start.x]);
            
            foreach (var node in startNodes)
            {
                ref var portal = ref portals[node.PortalKey];
                var startCell = new PathfindingCell(grid[portal.CenterPos.y * MapSizeX + portal.CenterPos.x])
                {
                    PortalKey = node.PortalKey,
                    GCost = node.Cost
                };
                startCell.HCost = Heuristic.GetHeuristic(startCell, endCell);
                open.Enqueue(startCell, startCell.GCost + startCell.HCost);
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
                int extLength = currentPortal.ExtIntCountElements >> (int)ExternalInternalLength.OffsetExtLength;
                for (int i = 0; i < extLength; i++)
                {
                    CheckConnection(grid, portals, getElement, currentPortal.ExternalPortalConnections[i], closedSet, currentCell, open, endCell, g + Heuristic.StraightCost);
                }
                
                //Check internal Connections
                int intLength = currentPortal.ExtIntCountElements & (int)ExternalInternalLength.InternalLength;
                for (int i = 0; i < intLength; i++)
                {
                    ref var connection = ref currentPortal.InternalPortalConnections[i];
                    var portalKey = Portal.GetPortalKeyFromInternalConnection(currentCell.PortalKey, connection.portalKey);
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
    
        private static void CheckConnection(Cell[] grid, Portal[] portals, Dictionary<int, PathfindingCell> getElement, int portalKey, HashSet<int> closedSet, PathfindingCell currentCell,
            FastPriorityQueue open, PathfindingCell goalCell, int g)
        {
            
            if (getElement.TryGetValue(portalKey, out var neighbour)){}
            else
            {
                ref var portal = ref portals[portalKey];
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
    
        private static List<PortalNode> FindPortalNodes(Portal[] portals, Cell[] grid, Chunk[] chunks, Vector2D start)
        {                  
            Vector2D chunkPos = new Vector2D(start.x / ChunkSize,
                start.y / ChunkSize);      
            int chunkId = chunkPos.x + ChunkMapSizeX * chunkPos.y;

            byte firstPortalPos = chunks[chunkId].FirstPortalKey;
            
            Vector2D min = new Vector2D(chunkPos.x * ChunkSize, chunkPos.y * ChunkSize);
            Vector2D max = min + new Vector2D(ChunkSize, ChunkSize);
            
            int firstPossiblePortalKeyInChunk = Portal.GeneratePortalKey(chunkId, 0, 0);
            List<PortalNode> nodes = new List<PortalNode>();
            
            //no path
            if(firstPortalPos == byte.MaxValue) return nodes;

            ushort[] costFields = BFS.FindAllCostsInChunkFromStartPos(grid, start, min, max);
            for (byte i = firstPortalPos; i < MainWindowViewModel.MaxPortalsInChunk; i++)
            {
                    ref var portal = ref portals[firstPossiblePortalKeyInChunk + i];
                    if(portal == null) continue;
                    var cost = BFS.GetCostForPath(costFields, portal.CenterPos);
                    if(cost == ushort.MaxValue) continue;
                    nodes.Add(new PortalNode(firstPossiblePortalKeyInChunk + i, cost));
                    //RetrieveConnectedPortalsToStartPos;
                    int length = portal.ExtIntCountElements & (int)ExternalInternalLength.InternalLength;
                    for (int j = 0; j < length; j++)
                    {
                        byte portalKey = portal.InternalPortalConnections[j].portalKey;
                        cost = BFS.GetCostForPath(costFields, portals[firstPossiblePortalKeyInChunk + portalKey].CenterPos);
                        nodes.Add(new PortalNode(firstPossiblePortalKeyInChunk + portalKey, cost));
                    }
                    break;
            }
            
            return nodes;
        }

        public static List<Vector2D> PortalsToPath(Cell[] grid, Portal[] portals, Vector2D pathStart, Vector2D pathEnd, List<int> pathAsPortals)
        {
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