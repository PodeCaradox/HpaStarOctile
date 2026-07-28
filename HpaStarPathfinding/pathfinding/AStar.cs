using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.utils;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class AStar
{
    //Reusable search state as plain parallel arrays (struct of arrays), one per thread:
    //a search touches only dense int/long arrays, no object references and no allocations per request
    private class SearchState
    {
        public int[] GCost = [];
        public int[] Parent = [];
        public int[] ClosedStamps = [];
        public PackedLongHeap Open = new(0);
        public int CurrentSearchId;

        public void EnsureCapacity(int cellCount)
        {
            if (GCost.Length >= cellCount) return;
            GCost = new int[cellCount];
            Parent = new int[cellCount];
            ClosedStamps = new int[cellCount];
            Open.EnsureCapacity(cellCount);
        }
    }

    private static readonly ThreadLocal<SearchState> SearchStateHolder = new(() => new SearchState());

    public static List<Vector2D> FindPath(Cell[] grid, Vector2D start, Vector2D end)
    {
        var state = SearchStateHolder.Value!;
        state.EnsureCapacity(grid.Length);
        int searchId = ++state.CurrentSearchId;
        var open = state.Open;
        var gCost = state.GCost;
        var parent = state.Parent;
        var closedStamps = state.ClosedStamps;
        open.Clear();

        //Search from end to start so the reconstructed path begins at start.
        int goalKey = ToKey(start);
        int startKey = ToKey(end);
        parent[startKey] = -1;
        gCost[startKey] = 0;
        open.Enqueue(startKey, 0);

        bool finished = false;
        while (open.Count > 0)
        {
            int currentKey = open.Dequeue();
            if (currentKey == goalKey)
            {
                finished = true;
                break;
            }

            closedStamps[currentKey] = searchId;
            byte connections = grid[currentKey].Connections;
            int x = currentKey % CorrectedMapSizeX;
            int y = currentKey / CorrectedMapSizeX;

            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                if ((connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE)
                    continue;

                var direction = DirectionsVector.AllDirections[i];
                int neighbourKey = (y + direction.y) * CorrectedMapSizeX + x + direction.x;
                if (closedStamps[neighbourKey] == searchId)
                    continue;

                int g = gCost[currentKey] + (i % 2 == 0 ? Heuristic.StraightCost : Heuristic.DiagonalCost);
                if (!open.Contains(neighbourKey))
                {
                    gCost[neighbourKey] = g;
                    parent[neighbourKey] = currentKey;
                    open.Enqueue(neighbourKey, g + Heuristic.GetHeuristic(ToVector(neighbourKey), start));
                }
                else if (g < gCost[neighbourKey])
                {
                    gCost[neighbourKey] = g;
                    parent[neighbourKey] = currentKey;
                    open.UpdatePriority(neighbourKey, g + Heuristic.GetHeuristic(ToVector(neighbourKey), start));
                }
            }
        }

        if (!finished) return [];

        var path = new List<Vector2D>();
        int key = goalKey;
        while (key >= 0)
        {
            path.Add(ToVector(key));
            key = parent[key];
        }

        return path;
    }

    private static int ToKey(Vector2D pos)
    {
        return pos.y * CorrectedMapSizeX + pos.x;
    }

    private static Vector2D ToVector(int key)
    {
        return new Vector2D(key % CorrectedMapSizeX, key / CorrectedMapSizeX);
    }
}
