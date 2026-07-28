using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.utils;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class HpaStar
{
    //Reusable search state as plain parallel arrays (struct of arrays), one per thread:
    //a search touches only dense int/long arrays, no object references and no allocations per request
    private class SearchState
    {
        public int[] GCost = [];
        public int[] Parent = [];
        public int[] ClosedStamps = [];
        public int[] GoalStamps = [];
        public PackedLongHeap Open = new(0);
        public readonly List<(int PortalKey, int Cost)> PortalNodes = [];
        public int CurrentSearchId;

        public void EnsureCapacity(int portalCount)
        {
            if (GCost.Length >= portalCount) return;
            GCost = new int[portalCount];
            Parent = new int[portalCount];
            ClosedStamps = new int[portalCount];
            GoalStamps = new int[portalCount];
            Open.EnsureCapacity(portalCount);
        }
    }

    private static readonly ThreadLocal<SearchState> SearchStateHolder = new(() => new SearchState());

    public static List<int> FindPath(Cell[] grid, Chunk[] chunks, Vector2D start, Vector2D end, byte regionPortalStart, byte regionPortalEnd)
    {
        var state = SearchStateHolder.Value!;
        state.EnsureCapacity(MaxPortalsInChunk * chunks.Length);
        int searchId = ++state.CurrentSearchId;
        var open = state.Open;
        var gCost = state.GCost;
        var parent = state.Parent;
        open.Clear();

        //Search from end to start so the reconstructed path begins at the start chunk.
        FindPortalNodes(state, chunks, grid, start, regionPortalStart);
        foreach (var (portalKey, _) in state.PortalNodes)
        {
            state.GoalStamps[portalKey] = searchId;
        }

        FindPortalNodes(state, chunks, grid, end, regionPortalEnd);
        foreach (var (portalKey, cost) in state.PortalNodes)
        {
            gCost[portalKey] = cost;
            parent[portalKey] = -1;
            open.Enqueue(portalKey, cost + Heuristic.GetHeuristic(GetPortalCenter(chunks, portalKey), start));
        }

        int foundKey = -1;
        while (open.Count > 0)
        {
            int currentKey = open.Dequeue();
            if (state.GoalStamps[currentKey] == searchId)
            {
                foundKey = currentKey;
                break;
            }

            state.ClosedStamps[currentKey] = searchId;
            ref var currentPortal = ref chunks[currentKey / MaxPortalsInChunk].portals[currentKey % MaxPortalsInChunk]!;

            //Check external Connections
            for (int i = 0; i < currentPortal.ExternalPortalCount; i++)
            {
                int externalKey = currentPortal.ExternalPortalConnections[i];
                ref var externalPortal = ref chunks[externalKey / MaxPortalsInChunk].portals[externalKey % MaxPortalsInChunk]!;
                //External edges cross one step between the two portal centres: straight costs 10, diagonal 14.
                int stepCost = Heuristic.GetHeuristic(currentPortal.CenterPos, externalPortal.CenterPos);
                CheckConnection(state, chunks, externalKey, currentKey, gCost[currentKey] + stepCost, searchId, start);
            }

            //Check internal Connections
            int firstPortalKey = Portal.GetPortalKeyFromInternalConnection(currentKey);
            for (int i = 0; i < currentPortal.InternalPortalCount; i++)
            {
                ref var con = ref currentPortal.InternalPortalConnections[i];
                CheckConnection(state, chunks, firstPortalKey + con.portalKey, currentKey, gCost[currentKey] + con.cost, searchId, start);
            }
        }

        if (foundKey < 0) return [];

        var path = new List<int>();
        int key = foundKey;
        while (key >= 0)
        {
            path.Add(key);
            key = parent[key];
        }

        return path;
    }

    private static void CheckConnection(SearchState state, Chunk[] chunks, int portalKey, int currentKey, int g, int searchId, Vector2D goalPos)
    {
        if (state.ClosedStamps[portalKey] == searchId) return;

        if (!state.Open.Contains(portalKey))
        {
            state.GCost[portalKey] = g;
            state.Parent[portalKey] = currentKey;
            state.Open.Enqueue(portalKey, g + Heuristic.GetHeuristic(GetPortalCenter(chunks, portalKey), goalPos));
        }
        else if (g < state.GCost[portalKey])
        {
            state.GCost[portalKey] = g;
            state.Parent[portalKey] = currentKey;
            state.Open.UpdatePriority(portalKey, g + Heuristic.GetHeuristic(GetPortalCenter(chunks, portalKey), goalPos));
        }
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
