using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.pathfinding.PathfindingCellTypes;
using HpaStarPathfinding.utils;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class AStar
{
    //Reusable search state, one per thread: no allocations per path request
    private class SearchState
    {
        public PathfindingCellAStar[] Nodes = [];
        public int[] ClosedStamps = [];
        public FastPriorityQueue<PathfindingCellAStar> Open = new(0);
        public int CurrentSearchId;

        public void EnsureCapacity(int cellCount)
        {
            if (Nodes.Length >= cellCount) return;
            Nodes = new PathfindingCellAStar[cellCount];
            ClosedStamps = new int[cellCount];
            Open = new FastPriorityQueue<PathfindingCellAStar>(cellCount);
        }
    }

    private static readonly ThreadLocal<SearchState> SearchStateHolder = new(() => new SearchState());

    public static List<Vector2D> FindPath(Cell[] grid, Vector2D start, Vector2D end)
    {
        var state = SearchStateHolder.Value!;
        state.EnsureCapacity(grid.Length);
        int searchId = ++state.CurrentSearchId;
        var open = state.Open;
        open.Clear();

        //Search from end to start so the reconstructed path begins at start.
        int goalKey = ToKey(start);
        Vector2D goalPos = start;

        var startNode = GetNode(state, grid, ToKey(end), searchId);
        open.Enqueue(startNode, 0);

        PathfindingCellAStar? currentCell = null;
        bool finished = false;
        while (open.Count > 0)
        {
            currentCell = open.Dequeue();
            int currentKey = currentCell.PoolIndex;
            int x = currentKey % CorrectedMapSizeX;
            int y = currentKey / CorrectedMapSizeX;

            if (currentKey == goalKey)
            {
                finished = true;
                break;
            }

            state.ClosedStamps[currentKey] = searchId;
            byte connections = currentCell.Connections;

            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                if ((connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE)
                    continue;

                var direction = DirectionsVector.AllDirections[i];
                int neighbourKey = (y + direction.y) * CorrectedMapSizeX + x + direction.x;
                if (state.ClosedStamps[neighbourKey] == searchId)
                    continue;

                var neighbour = GetNode(state, grid, neighbourKey, searchId);
                int g = currentCell.GCost + (i % 2 == 0 ? Heuristic.StraightCost : Heuristic.DiagonalCost);

                if (!open.Contains(neighbour))
                {
                    neighbour.GCost = g;
                    neighbour.HCost = Heuristic.GetHeuristic(ToVector(neighbourKey), goalPos);
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
        }

        if (!finished) return [];

        var path = new List<Vector2D>();
        while (currentCell != null)
        {
            path.Add(ToVector(currentCell.PoolIndex));
            currentCell = currentCell.Parent;
        }

        return path;
    }

    private static PathfindingCellAStar GetNode(SearchState state, Cell[] grid, int key, int searchId)
    {
        var node = state.Nodes[key] ??= new PathfindingCellAStar(key);
        if (node.SearchId != searchId)
            node.Reset(searchId, grid[key].Connections);
        return node;
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
