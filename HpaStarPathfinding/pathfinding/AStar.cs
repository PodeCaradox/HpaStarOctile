using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.pathfinding.PathfindingCellTypes;
using HpaStarPathfinding.utils;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class AStar
{
    public static List<Vector2D> FindPath(Cell[] grid, Vector2D start, Vector2D end)
    {
        FastPriorityQueue<PathfindingCellAStar> open = new FastPriorityQueue<PathfindingCellAStar>(CorrectedMapSizeX * CorrectedMapSizeY);
        //Search from end to start so the reconstructed path begins at start.
        int goalKey = start.y * CorrectedMapSizeX + start.x;
        Vector2D goalPos = grid[goalKey].Position;

        HashSet<int> closedSet = [];
        Dictionary<int, PathfindingCellAStar> getElement = new Dictionary<int, PathfindingCellAStar>();

        int startKey = end.y * CorrectedMapSizeX + end.x;
        var startNode = new PathfindingCellAStar(grid[startKey]);
        getElement.Add(startKey, startNode);
        open.Enqueue(startNode, 0);

        PathfindingCellAStar? currentCell = null;
        bool finished = false;
        while (open.Count > 0) 
        {
            currentCell = open.Dequeue();
            int x = currentCell.Position.x;
            int y = currentCell.Position.y;
            int currentKey = y * CorrectedMapSizeX + x;

            if (currentKey == goalKey)
            {
                finished = true;
                break;
            }

            closedSet.Add(currentKey);
            byte connections = currentCell.Connections;

            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                if ((connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE)
                    continue;

                var direction = DirectionsVector.AllDirections[i];
                int neighbourKey = (y + direction.y) * CorrectedMapSizeX + x + direction.x;
                if (closedSet.Contains(neighbourKey))
                    continue;

                if (!getElement.TryGetValue(neighbourKey, out var neighbour))
                {
                    neighbour = new PathfindingCellAStar(grid[neighbourKey]);
                    getElement.Add(neighbourKey, neighbour);
                }

                int g = currentCell.GCost + (i % 2 == 0 ? Heuristic.StraightCost : Heuristic.DiagonalCost);

                if (!open.Contains(neighbour))
                {
                    neighbour.GCost = g;
                    neighbour.HCost = Heuristic.GetHeuristic(neighbour.Position, goalPos);
                    neighbour.Parent = currentCell;
                    open.Enqueue(neighbour, neighbour.GCost + neighbour.HCost);
                } 
                else if (g + neighbour.HCost < neighbour.FCost) {
                    neighbour.GCost = g;
                    neighbour.Parent = currentCell;
                    open.UpdatePriority(neighbour, neighbour.GCost + neighbour.HCost);
                }
            }
        }

        if(!finished) return [];
            
        var path = new List<Vector2D>();
        while (currentCell != null) {
            path.Add(currentCell.Position);
            currentCell = currentCell.Parent;
        }

        return path;
    }
}
