using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding.PathfindingCellTypes;
using HpaStarPathfinding.utils;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class HpaStar
{
    
    private class NeighbourPortal(int portalKey, int cost)
    {
        public readonly int PortalKey = portalKey;
        public readonly int Cost = cost;
    }
        
    public static List<int> FindPath(Cell[] grid, Chunk[] chunks, Vector2D start, Vector2D end, byte regionPortalStart, byte regionPortalEnd)
    {
        var startNodes  = FindPortalNodes(chunks, grid, end, regionPortalEnd);
        HashSet<int> goalNodes  = new HashSet<int>(FindPortalNodes(chunks, grid, start, regionPortalStart).Select(x => x.PortalKey));
        //TODO maybe calc how many portals are currently on the Map less memory but more processing?
        FastPriorityQueue<PathfindingCellHpa> open = new FastPriorityQueue<PathfindingCellHpa>(MaxPortalsInChunk * ChunkMapSizeX * ChunkMapSizeY);
        HashSet<int> closedSet = [];
        Dictionary<int, PathfindingCellHpa> getElement = new Dictionary<int, PathfindingCellHpa>();
        Vector2D goalPos = grid[start.y * CorrectedMapSizeX + start.x].Position;
            
        foreach (var node in startNodes)
        {
            var chunkId = node.PortalKey / MaxPortalsInChunk;
            var portalId = node.PortalKey % MaxPortalsInChunk;
            
            ref var portal = ref chunks[chunkId].portals[portalId]!;
            var startCell = new PathfindingCellHpa(node.PortalKey)
            {
                GCost = node.Cost,
                HCost = Heuristic.GetHeuristic(portal.CenterPos, goalPos)
            };
            open.Enqueue(startCell, startCell.GCost + startCell.HCost);
            getElement.Add(startCell.PortalKey, startCell);
        }

        bool finished = false;
        PathfindingCellHpa? currentCell = null;
        while (open.Count > 0)
        {
            currentCell = open.Dequeue();
            if (goalNodes.Contains(currentCell.PortalKey))
            {
                finished = true;
                break;
            }
    
            var chunkId = currentCell.PortalKey / MaxPortalsInChunk;
            var portalId = currentCell.PortalKey % MaxPortalsInChunk;
            ref var currentPortal = ref chunks[chunkId].portals[portalId]!;
            
            closedSet.Add(currentCell.PortalKey);
    
            //Check external Connections
            for (int i = 0; i < currentPortal.ExternalPortalCount; i++)
            {
                CheckConnection(chunks, getElement, currentPortal.ExternalPortalConnections[i], closedSet, currentCell, open, goalPos, currentCell.GCost + Heuristic.StraightCost);
            }
                
            //Check internal Connections
            int firstPortalKey = Portal.GetPortalKeyFromInternalConnection(currentCell.PortalKey);
            for (int i = 0; i < currentPortal.InternalPortalCount; i++)
            {
                ref var con = ref currentPortal.InternalPortalConnections[i];
                CheckConnection(chunks, getElement, firstPortalKey + con.portalKey, closedSet, currentCell, open, goalPos, currentCell.GCost + con.cost);
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
    
    private static void CheckConnection(Chunk[] chunks, Dictionary<int, PathfindingCellHpa> getElement, int portalKey, HashSet<int> closedSet, PathfindingCellHpa currentCell,
        FastPriorityQueue<PathfindingCellHpa> open, Vector2D goalPos, int g)
    {
            
        if (getElement.TryGetValue(portalKey, out var neighbour)){}
        else
        {
            neighbour = new PathfindingCellHpa(portalKey);
            getElement.Add(portalKey, neighbour);
        }
        if (closedSet.Contains(portalKey)) return;
            
        if (!open.Contains(neighbour))
        {
            var chunkId = portalKey / MaxPortalsInChunk;
            var portalId = portalKey % MaxPortalsInChunk;
            
            neighbour.GCost = g;
            neighbour.HCost = Heuristic.GetHeuristic(chunks[chunkId].portals[portalId]!.CenterPos, goalPos);
            neighbour.Parent = currentCell;
            open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
        } 
        else if (g + neighbour.HCost < neighbour.FCost) {
            neighbour.GCost = g;
            neighbour.Parent = currentCell;
            open.UpdatePriority(neighbour, neighbour.GCost + neighbour.HCost);
        }
    }
    
    private static List<NeighbourPortal> FindPortalNodes(Chunk[] chunks, Cell[] grid, Vector2D start, byte region)
    {              
        int chunkId = start.x / ChunkSize + ChunkMapSizeX * (start.y / ChunkSize);
        ushort[] costFields = BFS.BfsFromStartPos(grid, start);
        int firstPossiblePortal = Portal.GeneratePortalKey(chunkId, 0, 0);
            
        List<NeighbourPortal> nodes = [];
        var portal = AddPortal(chunks, firstPossiblePortal, region, costFields, nodes);
        for (int j = 0; j < portal.InternalPortalCount; j++)
        {
            AddPortal(chunks, firstPossiblePortal, portal.InternalPortalConnections[j].portalKey, costFields, nodes);
        }
            
        return nodes;
    }

    private static Portal AddPortal(Chunk[] chunks, int firstPossiblePortal, byte portalKey,
        ushort[] costFields, List<NeighbourPortal> nodes)
    {
        int key = firstPossiblePortal + portalKey;
        var chunkId = key / MaxPortalsInChunk;
        
        ref var portal = ref chunks[chunkId].portals[portalKey]!;
        var cost = BFS.GetCostForPath(costFields, portal.CenterPos);
        nodes.Add(new NeighbourPortal(key, cost));
        return portal;
    }
}