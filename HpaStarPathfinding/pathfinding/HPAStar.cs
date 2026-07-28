using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding.PathfindingCellTypes;
using HpaStarPathfinding.utils;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class HpaStar
{
    //Reusable search state, one per thread: no allocations per path request
    private class SearchState
    {
        public PathfindingCellHpa?[] Nodes = [];
        public int[] ClosedStamps = [];
        public int[] GoalStamps = [];
        public FastPriorityQueue<PathfindingCellHpa> Open = new(0);
        public readonly List<(int PortalKey, int Cost)> PortalNodes = [];
        public int CurrentSearchId;

        public void EnsureCapacity(int portalCount)
        {
            if (Nodes.Length >= portalCount) return;
            Nodes = new PathfindingCellHpa?[portalCount];
            ClosedStamps = new int[portalCount];
            GoalStamps = new int[portalCount];
            Open = new FastPriorityQueue<PathfindingCellHpa>(portalCount);
        }
    }

    private static readonly ThreadLocal<SearchState> SearchStateHolder = new(() => new SearchState());

    public static List<int> FindPath(Cell[] grid, Chunk[] chunks, Vector2D start, Vector2D end, byte regionPortalStart, byte regionPortalEnd)
    {
        var state = SearchStateHolder.Value!;
        state.EnsureCapacity(MaxPortalsInChunk * chunks.Length);
        int searchId = ++state.CurrentSearchId;
        var open = state.Open;
        open.Clear();

        Vector2D goalPos = grid[start.y * CorrectedMapSizeX + start.x].Position;

        //Search from end to start so the reconstructed path begins at the start chunk.
        FindPortalNodes(state, chunks, grid, start, regionPortalStart);
        foreach (var (portalKey, _) in state.PortalNodes)
        {
            state.GoalStamps[portalKey] = searchId;
        }

        FindPortalNodes(state, chunks, grid, end, regionPortalEnd);
        foreach (var (portalKey, cost) in state.PortalNodes)
        {
            var startCell = GetNode(state, portalKey, searchId);
            startCell.GCost = cost;
            startCell.HCost = Heuristic.GetHeuristic(GetPortalCenter(chunks, portalKey), goalPos);
            open.Enqueue(startCell, startCell.GCost + startCell.HCost);
        }

        bool finished = false;
        PathfindingCellHpa? currentCell = null;
        while (open.Count > 0)
        {
            currentCell = open.Dequeue();
            if (state.GoalStamps[currentCell.PortalKey] == searchId)
            {
                finished = true;
                break;
            }

            var chunkId = currentCell.PortalKey / MaxPortalsInChunk;
            var portalId = currentCell.PortalKey % MaxPortalsInChunk;
            ref var currentPortal = ref chunks[chunkId].portals[portalId]!;

            state.ClosedStamps[currentCell.PortalKey] = searchId;

            //Check external Connections
            for (int i = 0; i < currentPortal.ExternalPortalCount; i++)
            {
                int externalKey = currentPortal.ExternalPortalConnections[i];
                ref var externalPortal = ref chunks[externalKey / MaxPortalsInChunk].portals[externalKey % MaxPortalsInChunk]!;
                //External edges cross one step between the two portal centres: straight costs 10, diagonal 14.
                int stepCost = Heuristic.GetHeuristic(currentPortal.CenterPos, externalPortal.CenterPos);
                CheckConnection(state, chunks, externalKey, currentCell, open, goalPos, currentCell.GCost + stepCost, searchId);
            }

            //Check internal Connections
            int firstPortalKey = Portal.GetPortalKeyFromInternalConnection(currentCell.PortalKey);
            for (int i = 0; i < currentPortal.InternalPortalCount; i++)
            {
                ref var con = ref currentPortal.InternalPortalConnections[i];
                CheckConnection(state, chunks, firstPortalKey + con.portalKey, currentCell, open, goalPos, currentCell.GCost + con.cost, searchId);
            }
        }

        if (!finished) return [];

        var path = new List<int>();
        while (currentCell != null)
        {
            path.Add(currentCell.PortalKey);
            currentCell = currentCell.Parent;
        }

        return path;
    }

    private static void CheckConnection(SearchState state, Chunk[] chunks, int portalKey, PathfindingCellHpa currentCell,
        FastPriorityQueue<PathfindingCellHpa> open, Vector2D goalPos, int g, int searchId)
    {
        if (state.ClosedStamps[portalKey] == searchId) return;
        var neighbour = GetNode(state, portalKey, searchId);

        if (!open.Contains(neighbour))
        {
            neighbour.GCost = g;
            neighbour.HCost = Heuristic.GetHeuristic(GetPortalCenter(chunks, portalKey), goalPos);
            neighbour.Parent = currentCell;
            open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
        }
        else if (g + neighbour.HCost < neighbour.FCost)
        {
            neighbour.GCost = g;
            neighbour.Parent = currentCell;
            open.UpdatePriority(neighbour, neighbour.GCost + neighbour.HCost);
        }
    }

    private static PathfindingCellHpa GetNode(SearchState state, int portalKey, int searchId)
    {
        var node = state.Nodes[portalKey] ??= new PathfindingCellHpa(portalKey);
        if (node.SearchId != searchId)
            node.Reset(searchId);
        return node;
    }

    private static Vector2D GetPortalCenter(Chunk[] chunks, int portalKey)
    {
        return chunks[portalKey / MaxPortalsInChunk].portals[portalKey % MaxPortalsInChunk]!.CenterPos;
    }

    //Collects every portal of the region the start cell belongs to, together with the cheapest cost to reach it.
    private static void FindPortalNodes(SearchState state, Chunk[] chunks, Cell[] grid, Vector2D start, byte region)
    {
        state.PortalNodes.Clear();
        int chunkId = start.x / ChunkSize + ChunkMapSizeX * (start.y / ChunkSize);
        ushort[] costFields = BFS.BfsFromStartPos(grid, start);
        int firstPossiblePortal = Portal.GeneratePortalKey(chunkId, 0, 0);

        ref var regionPortal = ref chunks[chunkId].portals[region]!;
        AddPortal(state, firstPossiblePortal, region, regionPortal, costFields);
        for (int j = 0; j < regionPortal.InternalPortalCount; j++)
        {
            byte portalKey = regionPortal.InternalPortalConnections[j].portalKey;
            AddPortal(state, firstPossiblePortal, portalKey, chunks[chunkId].portals[portalKey]!, costFields);
        }
    }

    private static void AddPortal(SearchState state, int firstPossiblePortal, byte portalKey,
        Portal portal, ushort[] costFields)
    {
        int key = firstPossiblePortal + portalKey;
        var cost = BFS.GetCostForPath(costFields, portal.CenterPos);
        state.PortalNodes.Add((key, cost));
    }
}
