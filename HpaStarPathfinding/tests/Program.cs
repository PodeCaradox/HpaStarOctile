using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.pathfinding;
using HpaStarPathfinding.pathfinding.PathfindingCache;
using HpaStarPathfinding.pathfinding.PathfindingCache.PathfindingResultTypes;
using HpaStarPathfinding.ViewModel;

//Headless verification of the pathfinding core: HPA* (incl. caches) vs plain A* on random maps,
//with dynamic wall edits and cache invalidation in between.
var rng = new Random(42);
int failures = 0;
double maxCostRatio = 1.0;

RunMapRound(50, 50, 0.25, 3, 150);
RunMapRound(200, 200, 0.25, 1, 100); //400 chunks: exercises 64-bit path id packing
RunStressRound(50, 50, 0.25, 1500);  //>1000 cached paths: exercises LRU eviction

Console.WriteLine($"Max HPA/A* cost ratio: {maxCostRatio:F2}");
Console.WriteLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECKS FAILED");
return failures == 0 ? 0 : 1;

void RunMapRound(int sizeX, int sizeY, double wallProbability, int mapCount, int requestsPerMap)
{
    SetMapSize(sizeX, sizeY);
    for (int m = 0; m < mapCount; m++)
    {
        var (map, chunks) = BuildMap(wallProbability);
        PathFindingManager.ClearCache();
        var fixedPairs = new List<(Vector2D Start, Vector2D Goal)>();

        for (int r = 0; r < requestsPerMap; r++)
        {
            var start = RandomCell();
            var goal = RandomCell();
            if (start == goal) continue;
            fixedPairs.Add((start, goal));
            VerifyRequest(map, chunks, start, goal);
        }

        //edit phase: toggle walls, rebuild the affected chunks and invalidate the cached paths
        for (int e = 0; e < 20; e++)
        {
            var pos = RandomCell();
            ToggleWall(map, pos);
            var dirtyChunks = new Dictionary<int, ChunkDirty>();
            Chunk.CheckWhichChunksAreDirty(ref dirtyChunks, pos);
            foreach (var (key, dirty) in dirtyChunks)
                Chunk.UpdateDirtyChunk(ref map, ref chunks[key], dirty);
            PathFindingManager.InvalidateChunks(dirtyChunks.Keys);
        }

        //re-request the same pairs on the changed map: stale cache entries would crash or return invalid paths here
        foreach (var (start, goal) in fixedPairs)
            VerifyRequest(map, chunks, start, goal);
    }
}

void RunStressRound(int sizeX, int sizeY, double wallProbability, int requestCount)
{
    SetMapSize(sizeX, sizeY);
    var (map, chunks) = BuildMap(wallProbability);
    PathFindingManager.ClearCache();
    for (int r = 0; r < requestCount; r++)
    {
        var start = RandomCell();
        var goal = RandomCell();
        if (start == goal) continue;
        VerifyRequest(map, chunks, start, goal);
    }
}

void VerifyRequest(Cell[] map, Chunk[] chunks, Vector2D start, Vector2D goal)
{
    var aStarPath = AStar.FindPath(map, start, goal);
    var hpaPath = HpaPath(map, chunks, start, goal);

    Check((aStarPath.Count > 0) == (hpaPath.Count > 0),
        $"existence mismatch {start} -> {goal}: A*={aStarPath.Count} waypoints, HPA={hpaPath.Count} waypoints");
    if (aStarPath.Count == 0 || hpaPath.Count == 0) return;

    Check(PathIsValid(map, aStarPath, start, goal), $"A* path invalid {start} -> {goal}");
    Check(PathIsValid(map, hpaPath, start, goal), $"HPA path invalid {start} -> {goal}");

    int aStarCost = PathCost(aStarPath);
    int hpaCost = PathCost(hpaPath);
    maxCostRatio = Math.Max(maxCostRatio, (double)hpaCost / aStarCost);
    //Cached high level paths are shared per chunk/region pair, so a portal detour compared to the
    //cell-optimal A* path is expected; it must stay bounded though.
    Check(hpaCost <= aStarCost * 3 + 200, $"HPA cost {hpaCost} much worse than A* cost {aStarCost} for {start} -> {goal}");

    //a second identical request must return the identical path (cache consistency)
    var cachedPath = HpaPath(map, chunks, start, goal);
    Check(hpaPath.SequenceEqual(cachedPath), $"cached path differs from first request for {start} -> {goal}");
}

List<Vector2D> HpaPath(Cell[] map, Chunk[] chunks, Vector2D start, Vector2D goal)
{
    PathfindingResult result = PathFindingManager.GetPath(map, chunks, start, goal);
    switch (result.Type)
    {
        case PathfindingType.NoPath:
            return [];
        case PathfindingType.HighLevelPath:
            var pathAsPortals = PathFindingManager.GetNextPath((result as HighLevelPathResult)!);
            return PathFindingManager.PortalsToPath(map, chunks, start, goal, pathAsPortals!);
        case PathfindingType.ShortPath:
            return PathFindingManager.GetNextPath((result as ShortPathResult)!);
        default:
            return [];
    }
}

void SetMapSize(int x, int y)
{
    MainWindowViewModel.MapSizeX = x;
    MainWindowViewModel.MapSizeY = y;
    MainWindowViewModel.CorrectedMapSizeX = (x + MainWindowViewModel.ChunkSize - 1) / MainWindowViewModel.ChunkSize * MainWindowViewModel.ChunkSize;
    MainWindowViewModel.CorrectedMapSizeY = (y + MainWindowViewModel.ChunkSize - 1) / MainWindowViewModel.ChunkSize * MainWindowViewModel.ChunkSize;
    MainWindowViewModel.ChunkMapSizeX = MainWindowViewModel.CorrectedMapSizeX / MainWindowViewModel.ChunkSize;
    MainWindowViewModel.ChunkMapSizeY = MainWindowViewModel.CorrectedMapSizeY / MainWindowViewModel.ChunkSize;
    PortalUtils.InitPortalUtilsValues();
}

(Cell[] Map, Chunk[] Chunks) BuildMap(double wallProbability)
{
    Chunk.ChunkIdCounter = 0;
    int mapSizeX = MainWindowViewModel.MapSizeX;
    int mapSizeY = MainWindowViewModel.MapSizeY;
    int sizeX = MainWindowViewModel.CorrectedMapSizeX;
    int sizeY = MainWindowViewModel.CorrectedMapSizeY;

    var map = new Cell[sizeY * sizeX];
    for (int y = 0; y < sizeY; y++)
        for (int x = 0; x < sizeX; x++)
            map[y * sizeX + x] = new Cell(new Vector2D(x, y));

    for (int y = 0; y < mapSizeY; y++)
        for (int x = 0; x < mapSizeX; x++)
            if (rng.NextDouble() < wallProbability)
                map[y * sizeX + x].Connections = DirectionsAsByte.NOT_WALKABLE;

    for (int y = 0; y < sizeY; y++)
        for (int x = 0; x < sizeX; x++)
        {
            if (x >= mapSizeX || y >= mapSizeY)
                map[y * sizeX + x].Connections = DirectionsAsByte.NOT_WALKABLE;
            else
                map[y * sizeX + x].UpdateConnection(map);
        }

    var chunks = new Chunk[MainWindowViewModel.ChunkMapSizeY * MainWindowViewModel.ChunkMapSizeX];
    for (int i = 0; i < chunks.Length; i++)
    {
        chunks[i] = new Chunk();
        Chunk.InitPortalsInChunk(ref map, ref chunks[i]);
    }

    return (map, chunks);
}

Vector2D RandomCell()
{
    return new Vector2D(rng.Next(MainWindowViewModel.MapSizeX), rng.Next(MainWindowViewModel.MapSizeY));
}

void ToggleWall(Cell[] map, Vector2D pos)
{
    var cell = map[pos.y * MainWindowViewModel.CorrectedMapSizeX + pos.x];
    cell.Connections = cell.Connections == DirectionsAsByte.NOT_WALKABLE
        ? DirectionsAsByte.WALKABLE
        : DirectionsAsByte.NOT_WALKABLE;
    cell.UpdateConnection(map);
}

bool PathIsValid(Cell[] map, List<Vector2D> path, Vector2D start, Vector2D goal)
{
    if (path.Count < 2) return false;
    if (path[0] != start || path[^1] != goal) return false;

    int sizeX = MainWindowViewModel.CorrectedMapSizeX;
    for (int i = 1; i < path.Count; i++)
    {
        int dx = path[i].x - path[i - 1].x;
        int dy = path[i].y - path[i - 1].y;
        int dirIndex = Array.FindIndex(DirectionsVector.AllDirections, d => d.x == dx && d.y == dy);
        if (dirIndex < 0) return false;
        var cell = map[path[i - 1].y * sizeX + path[i - 1].x];
        if ((cell.Connections & DirectionsAsByte.AllDirectionsAsByte[dirIndex]) != DirectionsAsByte.WALKABLE)
            return false;
    }

    return true;
}

int PathCost(List<Vector2D> path)
{
    int cost = 0;
    for (int i = 1; i < path.Count; i++)
    {
        int dx = Math.Abs(path[i].x - path[i - 1].x);
        int dy = Math.Abs(path[i].y - path[i - 1].y);
        cost += dx + dy == 2 ? Heuristic.DiagonalCost : Heuristic.StraightCost;
    }

    return cost;
}

void Check(bool condition, string message)
{
    if (condition) return;
    failures++;
    Console.WriteLine($"FAIL: {message}");
}
