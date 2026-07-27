using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding;
using HpaStarPathfinding.pathfinding.PathfindingCache;
using HpaStarPathfinding.pathfinding.PathfindingCache.PathfindingResultTypes;
using HpaStarPathfinding.ViewModel;
using Xunit;
using Xunit.Abstractions;

namespace HpaStarPathfinding.Tests;

/// <summary>
/// Verifies the search algorithms on random maps against an independent Dijkstra
/// reference implementation. A* must find a path exactly when one exists and the
/// path must be optimal; HPA* must find a valid path whenever one exists (its cost
/// is only reported, because the abstract graph can contain one-way diagonal
/// shortcuts that make it slightly suboptimal).
/// Every returned path is checked for correct endpoints and step-by-step legality
/// against the cell connection bits (bit set = blocked, the movement model the
/// search algorithms use).
/// </summary>
public class PathfindingCorrectnessTests(ITestOutputHelper output)
{
    private const int MapSize = 30;
    private const int SeedCount = 40;
    private const int QueriesPerSeed = 5;
    private const double BlockProbability = 0.25;

    [Fact]
    public void AStarAndHpaStar_OnRandomMaps_MatchDijkstraReference()
    {
        InitStaticMapValues();

        int suboptimalHpaPaths = 0;
        for (int seed = 0; seed < SeedCount; seed++)
        {
            var random = new Random(seed);
            var (map, chunks) = BuildWorld(random);
            var walkable = map
                .Where(c => c.Connections != DirectionsAsByte.NOT_WALKABLE &&
                            c.Position.x < MapSize && c.Position.y < MapSize)
                .Select(c => c.Position)
                .ToArray();

            for (int query = 0; query < QueriesPerSeed; query++)
            {
                var start = walkable[random.Next(walkable.Length)];
                var goal = walkable[random.Next(walkable.Length)];
                string config = $"seed {seed}: {start} -> {goal}";

                int? reference = Dijkstra(map, start, goal);

                var aStarPath = AStar.FindPath(map, start, goal);
                if (reference is null)
                {
                    Assert.True(aStarPath.Count == 0, $"A* found a path but none exists: {config}");
                    Assert.True(HpaStarFindPath(map, chunks, start, goal).Count == 0,
                        $"HPA* found a path but none exists: {config}");
                    continue;
                }

                Assert.True(aStarPath.Count > 0, $"A* found no path but one exists (cost {reference}): {config}");
                VerifyPath(map, aStarPath, start, goal, reference.Value, "A*", config);

                var hpaPath = HpaStarFindPath(map, chunks, start, goal);
                Assert.True(hpaPath.Count > 0, $"HPA* found no path but one exists (cost {reference}): {config}");
                int hpaCost = VerifyPath(map, hpaPath, start, goal, null, "HPA*", config);
                if (hpaCost > reference.Value)
                    suboptimalHpaPaths++;
            }
        }

        output.WriteLine($"{suboptimalHpaPaths} of {SeedCount * QueriesPerSeed} HPA* paths were longer than " +
                         "the Dijkstra optimum (informational, not asserted).");
    }

    /// <summary>
    /// Checks endpoints and step-by-step legality of a path and returns its cost.
    /// Consecutive duplicate positions are tolerated (portal path segments share joints).
    /// </summary>
    private static int VerifyPath(Cell[] map, List<Vector2D> path, Vector2D start, Vector2D goal,
        int? expectedCost, string algorithm, string config)
    {
        int width = MainWindowViewModel.CorrectedMapSizeX;
        Assert.True(path[0] == start, $"{algorithm} path starts at {path[0]}, expected {start}: {config}");
        Assert.True(path[^1] == goal, $"{algorithm} path ends at {path[^1]}, expected {goal}: {config}");

        int cost = 0;
        for (int i = 1; i < path.Count; i++)
        {
            int dx = path[i].x - path[i - 1].x;
            int dy = path[i].y - path[i - 1].y;
            if (dx == 0 && dy == 0)
                continue; // shared joint of two refined portal path segments

            int directionIndex = -1;
            for (int d = 0; d < DirectionsVector.AllDirections.Length; d++)
                if (DirectionsVector.AllDirections[d].x == dx && DirectionsVector.AllDirections[d].y == dy)
                    directionIndex = d;

            Assert.True(directionIndex >= 0,
                $"{algorithm} path step {path[i - 1]} -> {path[i]} is not a single grid step: {config}");
            Assert.True(
                (map[path[i - 1].y * width + path[i - 1].x].Connections & DirectionsAsByte.AllDirectionsAsByte[directionIndex]) ==
                DirectionsAsByte.WALKABLE,
                $"{algorithm} path step {path[i - 1]} -> {path[i]} crosses a blocked connection: {config}");
            cost += directionIndex % 2 == 0 ? 10 : 14;
        }

        if (expectedCost is { } expected)
            Assert.True(cost == expected, $"{algorithm} path costs {cost}, expected optimal {expected}: {config}");

        return cost;
    }

    /// <summary>
    /// Mirrors MainWindowViewModel.HpaStarFindPath.
    /// </summary>
    private static List<Vector2D> HpaStarFindPath(Cell[] map, Chunk[] chunks, Vector2D start, Vector2D goal)
    {
        PathfindingResult result = PathFindingManager.GetPath(map, chunks, start, goal);
        return result.Type switch
        {
            PathfindingType.NoPath => [],
            PathfindingType.HighLevelPath => PathFindingManager.PortalsToPath(map, chunks, start, goal,
                PathFindingManager.GetNextPath((HighLevelPathResult)result)!),
            PathfindingType.ShortPath => PathFindingManager.GetNextPath((ShortPathResult)result),
            _ => []
        };
    }

    /// <summary>
    /// Independent ground truth: Dijkstra over the grid, moving through the 8 direction
    /// bits of each cell (bit set = blocked), with costs 10 straight / 14 diagonal.
    /// Returns the shortest cost or null when the goal is unreachable.
    /// </summary>
    private static int? Dijkstra(Cell[] map, Vector2D start, Vector2D goal)
    {
        int width = MainWindowViewModel.CorrectedMapSizeX;
        int height = MainWindowViewModel.CorrectedMapSizeY;
        var dist = new Dictionary<int, int>();
        var open = new PriorityQueue<int, int>();
        int startKey = start.y * width + start.x;
        int goalKey = goal.y * width + goal.x;
        dist[startKey] = 0;
        open.Enqueue(startKey, 0);

        while (open.Count > 0)
        {
            if (!open.TryDequeue(out int current, out int priority) || current == goalKey)
                break;
            if (priority > dist[current])
                continue; // stale queue entry

            int x = current % width;
            int y = current / width;
            byte connections = map[current].Connections;
            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                if ((connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE)
                    continue;

                var dir = DirectionsVector.AllDirections[i];
                int newX = x + dir.x;
                int newY = y + dir.y;
                if (newX < 0 || newX >= width || newY < 0 || newY >= height)
                    continue;

                int neighbourKey = newY * width + newX;
                int newCost = dist[current] + (i % 2 == 0 ? 10 : 14);
                if (newCost >= dist.GetValueOrDefault(neighbourKey, int.MaxValue))
                    continue;

                dist[neighbourKey] = newCost;
                open.Enqueue(neighbourKey, newCost);
            }
        }

        return dist.TryGetValue(goalKey, out int cost) ? cost : null;
    }

    private static void InitStaticMapValues()
    {
        // Mirrors MainWindow.InitValues()
        MainWindowViewModel.MapSizeX = MapSize;
        MainWindowViewModel.MapSizeY = MapSize;
        MainWindowViewModel.CorrectedMapSizeX = (MapSize + MainWindowViewModel.ChunkSize - 1) / MainWindowViewModel.ChunkSize * MainWindowViewModel.ChunkSize;
        MainWindowViewModel.CorrectedMapSizeY = (MapSize + MainWindowViewModel.ChunkSize - 1) / MainWindowViewModel.ChunkSize * MainWindowViewModel.ChunkSize;
        MainWindowViewModel.ChunkMapSizeX = MainWindowViewModel.CorrectedMapSizeX / MainWindowViewModel.ChunkSize;
        MainWindowViewModel.ChunkMapSizeY = MainWindowViewModel.CorrectedMapSizeY / MainWindowViewModel.ChunkSize;
        PortalUtils.InitPortalUtilsValues();
    }

    /// <summary>
    /// Mirrors PortalConnectivityTests.BuildWorld with randomly blocked cells.
    /// </summary>
    private static (Cell[] map, Chunk[] chunks) BuildWorld(Random random)
    {
        var map = new Cell[MainWindowViewModel.CorrectedMapSizeY * MainWindowViewModel.CorrectedMapSizeX];
        for (int y = 0; y < MainWindowViewModel.CorrectedMapSizeY; y++)
        for (int x = 0; x < MainWindowViewModel.CorrectedMapSizeX; x++)
            map[y * MainWindowViewModel.CorrectedMapSizeX + x] = new Cell(new Vector2D(x, y));

        for (int y = 0; y < MapSize; y++)
        for (int x = 0; x < MapSize; x++)
            if (random.NextDouble() < BlockProbability)
                map[y * MainWindowViewModel.CorrectedMapSizeX + x].Connections = DirectionsAsByte.NOT_WALKABLE;

        foreach (var cell in map)
            if (cell.Connections != DirectionsAsByte.NOT_WALKABLE)
                cell.UpdateConnection(map);

        Chunk.ChunkIdCounter = 0;
        var chunks = new Chunk[MainWindowViewModel.ChunkMapSizeY * MainWindowViewModel.ChunkMapSizeX];
        for (int i = 0; i < chunks.Length; i++)
            chunks[i] = new Chunk();

        for (int i = 0; i < chunks.Length; i++)
            Chunk.InitPortalsInChunk(ref map, ref chunks[i]);

        return (map, chunks);
    }
}
