using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.model.pathfinding.PathfindingCellTypes;
using static HpaStarPathfinding.ViewModel.MainWindowViewModel;

namespace HpaStarPathfinding.pathfinding;

public static class AStar
{

    private class NeighbourCell(int cellKey, ushort cost)
    {
        public readonly int CellKey = cellKey;
        public readonly ushort GCost = cost;
    }

    private static List<NeighbourCell> GetNeighbours(PathfindingCellAStar cell)
    {
        List<NeighbourCell> neighbours = [];
        int i = 0;
        foreach (var direction in DirectionsVector.AllDirections)
        {
            if ((cell.Connections & DirectionsAsByte.AllDirectionsAsByte[i]) == DirectionsAsByte.WALKABLE)
            {
                int newX = cell.Position.x + direction.x;
                int newY = cell.Position.y + direction.y;
                neighbours.Add(new NeighbourCell(newY * CorrectedMapSizeX + newX, i % 2 == 0? Heuristic.StraightCost : Heuristic.DiagonalCost));
            }

            i++;
        }

        return neighbours;
    }

    public static List<Vector2D> FindPath(Cell[] grid, Vector2D start, Vector2D end)
    {
        FastPriorityQueue<PathfindingCellAStar> open = new FastPriorityQueue<PathfindingCellAStar>(CorrectedMapSizeX * CorrectedMapSizeY);
        Cell startCell = grid[end.y * CorrectedMapSizeX + end.x];
        PathfindingCellAStar goalCell  = new PathfindingCellAStar(grid[start.y * CorrectedMapSizeX + start.x]);
            
        HashSet<int> closedSet = [];
        Dictionary<int, PathfindingCellAStar> getElement = new Dictionary<int, PathfindingCellAStar>();

        open.Enqueue(new PathfindingCellAStar(startCell), 0);

        PathfindingCellAStar? currentCell = null;
        bool finished = false;
        while (open.Count > 0) 
        {
            currentCell = open.Dequeue();

            if (currentCell.Position == goalCell.Position)
            {
                finished = true;
                break;
            }

            closedSet.Add(currentCell.Position.x + currentCell.Position.y * CorrectedMapSizeX);
                
            foreach (var neighbourKey in GetNeighbours(currentCell))
            {
                if (getElement.TryGetValue(neighbourKey.CellKey, out var neighbour)){}
                else
                {
                    neighbour = new PathfindingCellAStar(grid[neighbourKey.CellKey]); 
                    getElement.Add(neighbourKey.CellKey, neighbour);
                }
                int g = currentCell.GCost + neighbourKey.GCost;
                    
                if (closedSet.Contains(neighbour.Position.x + neighbour.Position.y * CorrectedMapSizeX))
                    continue;
                   
                if (!open.Contains(neighbour))
                {
                    neighbour.GCost = g;
                    neighbour.HCost = Heuristic.GetHeuristic(neighbour.Position, goalCell.Position);
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