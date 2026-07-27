using System.Text;
using HpaStarPathfinding.model.map;
using HpaStarPathfinding.model.math;
using HpaStarPathfinding.model.pathfinding;
using HpaStarPathfinding.ViewModel;
using Xunit;
using Xunit.Abstractions;

namespace HpaStarPathfinding.Tests;

/// <summary>
/// Verifies that, inside every chunk, all portals connect with each other correctly
/// for every possible blocking combination of the cells around every chunk corner.
///
/// For each chunk and each of its four corners, every subset of the 3x3 cell block
/// around the corner cell is blocked in turn (the 3x3 block covers the corner cell,
/// its two neighbours inside the chunk, and the cells across the chunk borders that
/// influence the corner/diagonal portal creation). For each resulting map the portals
/// and their internal connections are rebuilt through the production code path
/// (<see cref="Chunk.InitPortalsInChunk"/>) and then checked for every portal pair:
/// an internal connection must exist exactly when the portal centre cells are
/// reachable from each other inside the chunk (independent flood-fill ground truth),
/// connections must be symmetric with equal costs, and both portals must share the
/// same region stamp exactly when they are connected.
///
/// External connections are checked across the same configurations: every external
/// connection must reference an existing portal in an adjacent chunk, and every
/// external edge must have a return path through the directed abstract graph
/// (internal + external edges, compared via strongly connected components) —
/// a one-way external edge would make HPA* lose paths. Deviations from strict
/// pairwise mirroring are only counted and reported, because diagonal corner
/// portals intentionally replace straight corner portals
/// (<see cref="PortalUtils"/>), so a pairwise mirror does not always exist.
/// </summary>
public class PortalConnectivityTests(ITestOutputHelper output)
{
    private const int MapSize = 30; // 3x3 chunks, so the centre chunk has all 8 neighbour chunks
    private const int ChunkSize = MainWindowViewModel.ChunkSize;
    private const int MaxStoredFailureMessages = 50;

    private static readonly (string Name, int OffsetX, int OffsetY)[] Corners =
    [
        ("NW", 0, 0),
        ("NE", ChunkSize - 1, 0),
        ("SW", 0, ChunkSize - 1),
        ("SE", ChunkSize - 1, ChunkSize - 1)
    ];

    private int _failureCount;
    private int _unmirroredExternalEdgeCount;
    private readonly List<string> _failureMessages = [];

    [Fact]
    public void PortalsConnectWithinChunk_ForEveryCornerBlockingCombination()
    {
        InitStaticMapValues();

        int configurations = 0;
        for (int chunkY = 0; chunkY < MainWindowViewModel.ChunkMapSizeY; chunkY++)
        for (int chunkX = 0; chunkX < MainWindowViewModel.ChunkMapSizeX; chunkX++)
        foreach (var corner in Corners)
        {
            var cornerCells = GetCornerBlockCells(chunkX, chunkY, corner);
            int combinationCount = 1 << cornerCells.Count;
            for (int mask = 0; mask < combinationCount; mask++)
            {
                configurations++;
                var blockedCells = new HashSet<Vector2D>();
                for (int bit = 0; bit < cornerCells.Count; bit++)
                    if ((mask & (1 << bit)) != 0)
                        blockedCells.Add(cornerCells[bit]);

                string config = $"chunk ({chunkX},{chunkY}) {corner.Name}-corner, " +
                                $"blocked: {string.Join(' ', blockedCells.Select(c => c.ToString()))}";
                try
                {
                    var (map, chunks) = BuildWorld(blockedCells);
                    VerifyAllChunks(map, chunks, config);
                }
                catch (Exception e)
                {
                    AddFailure($"{config}: threw {e.GetType().Name}: {e.Message}");
                }
            }
        }

        output.WriteLine($"Checked {configurations} corner-blocking configurations.");
        output.WriteLine($"{_unmirroredExternalEdgeCount} external connections were not pairwise-mirrored " +
                         "(informational; the return-path check is the authoritative one).");
        Assert.True(_failureCount == 0,
            $"{_failureCount} connectivity failures across {configurations} configurations. " +
            $"First {_failureMessages.Count}:\n" + string.Join('\n', _failureMessages));
    }

    /// <summary>
    /// Sanity guard for the sweep: on a fully walkable map every chunk must have
    /// portals and they must all be connected, so the corner-blocking sweep above
    /// can never pass vacuously because no portals were created.
    /// </summary>
    [Fact]
    public void OpenMap_EveryChunkHasConnectedPortals()
    {
        InitStaticMapValues();
        var (_, chunks) = BuildWorld([]);
        foreach (var chunk in chunks)
        {
            var portals = chunk.portals.Where(p => p is not null).ToList();
            Assert.True(portals.Count >= 4,
                $"chunk {chunk.ChunkId} has only {portals.Count} portals on an open map");
            foreach (var portal in portals)
                Assert.True(portal!.InternalPortalCount > 0,
                    $"chunk {chunk.ChunkId} portal {portal.CenterPos} has no internal connections on an open map");
        }
    }

    /// <summary>
    /// Reproduces the UI editing flow: cells are blocked/unblocked one at a time through
    /// the incremental pipeline (CheckWhichChunksAreDirty + UpdateDirtyChunk), exactly like
    /// MainWindow.ChangeMapCell. After every step all external connections must be valid
    /// (a dangling one crashes the app), and the resulting state must be identical to a
    /// full rebuild of the same map.
    /// </summary>
    [Fact]
    public void IncrementalWallTogglesMatchFullRebuild_ForEveryCornerBlockingCombination()
    {
        InitStaticMapValues();
        ResetFailureState();

        int configurations = 0;
        for (int chunkY = 0; chunkY < MainWindowViewModel.ChunkMapSizeY; chunkY++)
        for (int chunkX = 0; chunkX < MainWindowViewModel.ChunkMapSizeX; chunkX++)
        foreach (var corner in Corners)
        {
            var cornerCells = GetCornerBlockCells(chunkX, chunkY, corner);
            int combinationCount = 1 << cornerCells.Count;
            for (int mask = 0; mask < combinationCount; mask++)
            {
                configurations++;
                var blockedCells = new List<Vector2D>();
                for (int bit = 0; bit < cornerCells.Count; bit++)
                    if ((mask & (1 << bit)) != 0)
                        blockedCells.Add(cornerCells[bit]);

                string config = $"chunk ({chunkX},{chunkY}) {corner.Name}-corner, " +
                                $"blocked: {string.Join(' ', blockedCells.Select(c => c.ToString()))}";
                try
                {
                    var (map, chunks) = BuildWorld([]);
                    foreach (var pos in blockedCells)
                    {
                        ApplyWallChangeIncremental(map, chunks, pos, DirectionsAsByte.NOT_WALKABLE);
                        VerifyExternalValidity(chunks, $"{config} [after blocking {pos}]");
                    }

                    var (_, fullRebuildChunks) = BuildWorld(new HashSet<Vector2D>(blockedCells));
                    VerifyAllChunks(map, chunks, config + " [incremental state]");
                    VerifySameWorldState(fullRebuildChunks, chunks, config + " [after blocking]");

                    foreach (var pos in blockedCells)
                    {
                        ApplyWallChangeIncremental(map, chunks, pos, DirectionsAsByte.WALKABLE);
                        VerifyExternalValidity(chunks, $"{config} [after unblocking {pos}]");
                    }

                    //A fully enclosed cell keeps all connection bits set even after its neighbours
                    //are unblocked (it can never change back through neighbour edits), so after
                    //unblocking the incremental state legitimately differs from a fresh empty world.
                    if (FullyEnclosesSomeCell(blockedCells))
                        continue;

                    var (_, emptyChunks) = BuildWorld([]);
                    VerifySameWorldState(emptyChunks, chunks, config + " [after unblocking]");
                }
                catch (Exception e)
                {
                    AddFailure($"{config}: threw {e.GetType().Name}: {e.Message}");
                }
            }
        }

        output.WriteLine($"Checked {configurations} corner-blocking configurations incrementally.");
        Assert.True(_failureCount == 0,
            $"{_failureCount} incremental-update failures across {configurations} configurations. " +
            $"First {_failureMessages.Count}:\n" + string.Join('\n', _failureMessages));
    }

    /// <summary>
    /// Exercises the cell-border editor (MainWindow.ChangeCellBorderClicked): every single
    /// direction bit of every corner-block cell is toggled and toggled back through the
    /// incremental pipeline, on the empty map and on every single-cell-blocked baseline.
    /// External connections must stay valid after every toggle, and toggling back must
    /// restore the exact baseline state.
    /// </summary>
    [Fact]
    public void IncrementalCellBorderTogglesKeepPortalsValid_ForCornerCellsInAllDirections()
    {
        InitStaticMapValues();
        ResetFailureState();

        int configurations = 0;
        for (int chunkY = 0; chunkY < MainWindowViewModel.ChunkMapSizeY; chunkY++)
        for (int chunkX = 0; chunkX < MainWindowViewModel.ChunkMapSizeX; chunkX++)
        foreach (var corner in Corners)
        {
            var cornerCells = GetCornerBlockCells(chunkX, chunkY, corner);
            var masks = new List<int> { 0 };
            for (int bit = 0; bit < cornerCells.Count; bit++)
                masks.Add(1 << bit);

            foreach (int mask in masks)
            {
                configurations++;
                var blockedCells = new HashSet<Vector2D>();
                for (int bit = 0; bit < cornerCells.Count; bit++)
                    if ((mask & (1 << bit)) != 0)
                        blockedCells.Add(cornerCells[bit]);

                string config = $"chunk ({chunkX},{chunkY}) {corner.Name}-corner, " +
                                $"baseline blocked: {string.Join(' ', blockedCells.Select(c => c.ToString()))}";
                try
                {
                    var (map, chunks) = BuildWorld(blockedCells);
                    var baseline = SnapshotWorld(chunks);

                    foreach (var cell in cornerCells)
                    {
                        for (int directionIndex = 0; directionIndex < DirectionsVector.AllDirections.Length; directionIndex++)
                        {
                            var dir = DirectionsVector.AllDirections[directionIndex];
                            int neighbourX = cell.x + dir.x;
                            int neighbourY = cell.y + dir.y;
                            if (neighbourX < 0 || neighbourX >= MapSize || neighbourY < 0 || neighbourY >= MapSize)
                                continue; // out-of-map bits are never read and skip the rebuild in production

                            string toggleConfig = $"{config}, toggle {(DirtyDirections)directionIndex} of {cell}";
                            ToggleCellBorderIncremental(map, chunks, cell, directionIndex);
                            VerifyExternalValidity(chunks, toggleConfig + " [toggled]");
                            ToggleCellBorderIncremental(map, chunks, cell, directionIndex);
                            VerifyExternalValidity(chunks, toggleConfig + " [toggled back]");

                            var restored = SnapshotWorld(chunks);
                            for (int c = 0; c < restored.Length; c++)
                                if (restored[c] != baseline[c])
                                    AddFailure($"{toggleConfig}: chunk {c} state did not return to baseline after toggling back. " +
                                               $"Baseline: {baseline[c]} Restored: {restored[c]}");
                        }
                    }
                }
                catch (Exception e)
                {
                    AddFailure($"{config}: threw {e.GetType().Name}: {e.Message}");
                }
            }
        }

        output.WriteLine($"Checked {configurations} border-toggle baselines.");
        Assert.True(_failureCount == 0,
            $"{_failureCount} border-toggle failures across {configurations} baselines. " +
            $"First {_failureMessages.Count}:\n" + string.Join('\n', _failureMessages));
    }

    /// <summary>
    /// Regression test for the reported crash: block cell (20,9) (the SW corner cell of
    /// chunk 2), then open only its South connection through the cell-border editor so the
    /// cell becomes 0b_1110_1111. The diagonal corner portal of chunk 5 at (20,10) must not
    /// keep an external connection to a South-edge portal of chunk 2 at (21,9), because
    /// chunk 2 does not create a single portal there: (21,9) connects straight across to
    /// (21,10), so it merges into the big South portal.
    /// </summary>
    [Fact]
    public void BlockedCornerCellWithOnlySouthConnectionOpen_HasNoDanglingExternalConnection()
    {
        InitStaticMapValues();
        ResetFailureState();

        var (map, chunks) = BuildWorld([]);
        var cornerCell = new Vector2D(20, 9);

        ApplyWallChangeIncremental(map, chunks, cornerCell, DirectionsAsByte.NOT_WALKABLE);
        Assert.Equal(DirectionsAsByte.NOT_WALKABLE, map[9 * MainWindowViewModel.CorrectedMapSizeX + 20].Connections);

        ToggleCellBorderIncremental(map, chunks, cornerCell, 4); // open S (0,1)
        Assert.Equal(0b_1110_1111, map[9 * MainWindowViewModel.CorrectedMapSizeX + 20].Connections);

        VerifyExternalValidity(chunks, "cell (20,9) blocked with only its South connection open");
        Assert.True(_failureCount == 0, string.Join('\n', _failureMessages));
    }

    /// <summary>
    /// Regression test for the reported wrong portal: block the row (0..9, 10) and cell
    /// (9,9), then open only the NW connection of the blocked (4,10) so it becomes
    /// 0b_0111_1111. The South-edge scan of chunk 0 must not leave a phantom mini portal
    /// behind: cell (3,9) gets its SE diagonal opened towards (4,10) and has E open, but
    /// the next edge cell (4,9) is sealed straight across, so no portal can grow there.
    /// </summary>
    [Fact]
    public void BlockedRowWithSingleDiagonalDoor_HasNoPhantomMiniPortal()
    {
        InitStaticMapValues();
        ResetFailureState();

        var blockedCells = new HashSet<Vector2D>();
        for (int x = 0; x < 10; x++)
            blockedCells.Add(new Vector2D(x, 10));
        blockedCells.Add(new Vector2D(9, 9));

        var (map, chunks) = BuildWorld(blockedCells);

        // Open NW of the blocked (4,10) through the cell-border editor: 0b_1111_1111 ^ NW = 127.
        ToggleCellBorderIncremental(map, chunks, new Vector2D(4, 10), 7);
        Assert.Equal(0b_0111_1111, map[10 * MainWindowViewModel.CorrectedMapSizeX + 4].Connections);

        VerifyExternalValidity(chunks, "row (0..9,10) and (9,9) blocked, (4,10) with only NW open");
        Assert.True(_failureCount == 0, string.Join('\n', _failureMessages));
    }

    /// <summary>
    /// True when the blocked set seals off some other cell: every in-map neighbour of that
    /// cell is blocked (map edges count as blocked). Such a fully enclosed cell can never
    /// change back through neighbour edits — it behaves like a wall / an outside-map cell.
    /// This also holds for a blocked cell that is unblocked first: while all its neighbours
    /// are still walls, unblocking it immediately sets every bit again.
    /// </summary>
    private static bool FullyEnclosesSomeCell(List<Vector2D> blockedCells)
    {
        var blocked = new HashSet<Vector2D>(blockedCells);
        var candidates = new HashSet<Vector2D>(blockedCells);
        foreach (var pos in blockedCells)
        foreach (var dir in DirectionsVector.AllDirections)
        {
            int x = pos.x + dir.x;
            int y = pos.y + dir.y;
            if (x >= 0 && x < MapSize && y >= 0 && y < MapSize)
                candidates.Add(new Vector2D(x, y));
        }

        foreach (var candidate in candidates)
        {
            int firstUnblockedNeighbour = int.MaxValue;
            bool enclosed = true;
            foreach (var dir in DirectionsVector.AllDirections)
            {
                int nx = candidate.x + dir.x;
                int ny = candidate.y + dir.y;
                if (nx < 0 || nx >= MapSize || ny < 0 || ny >= MapSize)
                    continue; // the map edge always blocks this side

                var neighbour = new Vector2D(nx, ny);
                if (!blocked.Contains(neighbour))
                {
                    enclosed = false;
                    break;
                }

                firstUnblockedNeighbour = Math.Min(firstUnblockedNeighbour, blockedCells.IndexOf(neighbour));
            }

            if (!enclosed)
                continue;

            if (!blocked.Contains(candidate) || blockedCells.IndexOf(candidate) < firstUnblockedNeighbour)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Mirrors MainWindow.ChangeMapCell + RebuildTiles + RebuildPortals for one cell.
    /// </summary>
    private static void ApplyWallChangeIncremental(Cell[] map, Chunk[] chunks, Vector2D pos, byte newValue)
    {
        ref var cell = ref map[pos.y * MainWindowViewModel.CorrectedMapSizeX + pos.x];
        cell.Connections = newValue;
        cell.UpdateConnection(map);
        UpdateDirtyChunksIncremental(map, chunks, pos);
    }

    /// <summary>
    /// Mirrors MainWindow.ChangeCellBorderClicked for one direction bit of one cell.
    /// </summary>
    private static void ToggleCellBorderIncremental(Cell[] map, Chunk[] chunks, Vector2D pos, int directionIndex)
    {
        byte direction = (byte)(1 << directionIndex);
        var dir = DirectionsVector.AllDirections[directionIndex];
        ref var cell = ref map[pos.y * MainWindowViewModel.CorrectedMapSizeX + pos.x];
        cell.Connections = (byte)(cell.Connections ^ direction);
        ref var otherCell = ref map[(pos.y + dir.y) * MainWindowViewModel.CorrectedMapSizeX + pos.x + dir.x];
        otherCell.Connections = (byte)(otherCell.Connections ^ Cell.RotateLeft(direction, 4));
        UpdateDirtyChunksIncremental(map, chunks, pos);
    }

    private static void UpdateDirtyChunksIncremental(Cell[] map, Chunk[] chunks, Vector2D pos)
    {
        var dirtyChunks = new Dictionary<int, ChunkDirty>();
        Chunk.CheckWhichChunksAreDirty(ref dirtyChunks, pos);
        foreach (var (chunkKey, chunkDirty) in dirtyChunks)
            Chunk.UpdateDirtyChunk(ref map, ref chunks[chunkKey], chunkDirty);
    }

    /// <summary>
    /// The crash class from the UI: an external connection whose target portal does not exist.
    /// </summary>
    private void VerifyExternalValidity(Chunk[] chunks, string config)
    {
        foreach (var chunk in chunks)
        for (byte key = 0; key < MainWindowViewModel.MaxPortalsInChunk; key++)
        {
            var portal = chunk.portals[key];
            if (portal is null || portal.CenterPos is null) continue;
            for (int i = 0; i < portal.ExternalPortalCount; i++)
            {
                int externalKey = portal.ExternalPortalConnections[i];
                if (externalKey < 0)
                {
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos}: " +
                               $"external connection key {externalKey} is negative");
                    continue;
                }

                int targetChunkId = externalKey / MainWindowViewModel.MaxPortalsInChunk;
                int targetSlot = externalKey % MainWindowViewModel.MaxPortalsInChunk;
                if (targetChunkId >= chunks.Length || chunks[targetChunkId].portals[targetSlot] is null)
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos}: " +
                               $"dangling external connection {externalKey} " +
                               $"(chunk {targetChunkId} has no portal {targetSlot})");
            }
        }
    }

    /// <summary>
    /// The incrementally updated world must be identical to a full rebuild of the same map.
    /// </summary>
    private void VerifySameWorldState(Chunk[] expected, Chunk[] actual, string config)
    {
        for (int c = 0; c < expected.Length; c++)
        {
            if (!expected[c].regions.SequenceEqual(actual[c].regions))
                AddFailure($"{config}: chunk {c}: regions differ");

            for (byte slot = 0; slot < MainWindowViewModel.MaxPortalsInChunk; slot++)
            {
                var expectedPortal = expected[c].portals[slot];
                var actualPortal = actual[c].portals[slot];
                if (expectedPortal is null || actualPortal is null)
                {
                    if ((expectedPortal is null) != (actualPortal is null))
                        AddFailure($"{config}: chunk {c} slot {slot}: portal {(expectedPortal ?? actualPortal)!.CenterPos} " +
                                   $"exists only in the {(expectedPortal is null ? "incremental" : "full rebuild")} state");
                    continue;
                }

                if (expectedPortal.CenterPos != actualPortal.CenterPos ||
                    expectedPortal.Length != actualPortal.Length ||
                    expectedPortal.Offset != actualPortal.Offset)
                    AddFailure($"{config}: chunk {c} slot {slot}: full rebuild portal " +
                               $"{expectedPortal.CenterPos} L{expectedPortal.Length} O{expectedPortal.Offset} vs " +
                               $"incremental portal {actualPortal.CenterPos} L{actualPortal.Length} O{actualPortal.Offset}");

                var expectedExternals = expectedPortal.ExternalPortalConnections
                    .Take(expectedPortal.ExternalPortalCount).OrderBy(k => k).ToList();
                var actualExternals = actualPortal.ExternalPortalConnections
                    .Take(actualPortal.ExternalPortalCount).OrderBy(k => k).ToList();
                if (!expectedExternals.SequenceEqual(actualExternals))
                    AddFailure($"{config}: chunk {c} slot {slot} {expectedPortal.CenterPos}: " +
                               $"external connections full rebuild [{string.Join(' ', expectedExternals)}] vs " +
                               $"incremental [{string.Join(' ', actualExternals)}]");

                var expectedInternals = expectedPortal.InternalPortalConnections
                    .Take(expectedPortal.InternalPortalCount).Select(conn => $"{conn.portalKey}:{conn.cost}").OrderBy(s => s).ToList();
                var actualInternals = actualPortal.InternalPortalConnections
                    .Take(actualPortal.InternalPortalCount).Select(conn => $"{conn.portalKey}:{conn.cost}").OrderBy(s => s).ToList();
                if (!expectedInternals.SequenceEqual(actualInternals))
                    AddFailure($"{config}: chunk {c} slot {slot} {expectedPortal.CenterPos}: " +
                               $"internal connections full rebuild [{string.Join(' ', expectedInternals)}] vs " +
                               $"incremental [{string.Join(' ', actualInternals)}]");
            }
        }
    }

    private static string[] SnapshotWorld(Chunk[] chunks)
    {
        var snapshot = new string[chunks.Length];
        for (int c = 0; c < chunks.Length; c++)
        {
            var builder = new StringBuilder();
            builder.Append(string.Join(',', chunks[c].regions));
            for (byte slot = 0; slot < MainWindowViewModel.MaxPortalsInChunk; slot++)
            {
                var portal = chunks[c].portals[slot];
                if (portal is null) continue;
                builder.Append($";{slot}@{portal.CenterPos}L{portal.Length}O{portal.Offset}");
                builder.Append("E[").Append(string.Join('+', portal.ExternalPortalConnections
                    .Take(portal.ExternalPortalCount).OrderBy(k => k))).Append(']');
                builder.Append("I[").Append(string.Join('+', portal.InternalPortalConnections
                    .Take(portal.InternalPortalCount).Select(conn => $"{conn.portalKey}:{conn.cost}").OrderBy(s => s))).Append(']');
            }

            snapshot[c] = builder.ToString();
        }

        return snapshot;
    }

    private void ResetFailureState()
    {
        _failureCount = 0;
        _failureMessages.Clear();
        _unmirroredExternalEdgeCount = 0;
    }

    private static void InitStaticMapValues()
    {
        // Mirrors MainWindow.InitValues()
        MainWindowViewModel.MapSizeX = MapSize;
        MainWindowViewModel.MapSizeY = MapSize;
        MainWindowViewModel.CorrectedMapSizeX = (MapSize + ChunkSize - 1) / ChunkSize * ChunkSize;
        MainWindowViewModel.CorrectedMapSizeY = (MapSize + ChunkSize - 1) / ChunkSize * ChunkSize;
        MainWindowViewModel.ChunkMapSizeX = MainWindowViewModel.CorrectedMapSizeX / ChunkSize;
        MainWindowViewModel.ChunkMapSizeY = MainWindowViewModel.CorrectedMapSizeY / ChunkSize;
        PortalUtils.InitPortalUtilsValues();
    }

    /// <summary>
    /// The 3x3 block of cells around the chunk's corner cell, clipped to the map.
    /// </summary>
    private static List<Vector2D> GetCornerBlockCells(int chunkX, int chunkY, (string Name, int OffsetX, int OffsetY) corner)
    {
        int cornerX = chunkX * ChunkSize + corner.OffsetX;
        int cornerY = chunkY * ChunkSize + corner.OffsetY;
        var cells = new List<Vector2D>();
        for (int y = cornerY - 1; y <= cornerY + 1; y++)
        for (int x = cornerX - 1; x <= cornerX + 1; x++)
            if (x >= 0 && x < MapSize && y >= 0 && y < MapSize)
                cells.Add(new Vector2D(x, y));
        return cells;
    }

    private static (Cell[] map, Chunk[] chunks) BuildWorld(HashSet<Vector2D> blockedCells)
    {
        // Mirrors MainWindowViewModel.InitMap(): walls first, then connection bits
        var map = new Cell[MainWindowViewModel.CorrectedMapSizeY * MainWindowViewModel.CorrectedMapSizeX];
        for (int y = 0; y < MainWindowViewModel.CorrectedMapSizeY; y++)
        for (int x = 0; x < MainWindowViewModel.CorrectedMapSizeX; x++)
            map[y * MainWindowViewModel.CorrectedMapSizeX + x] = new Cell(new Vector2D(x, y));

        foreach (var pos in blockedCells)
            map[pos.y * MainWindowViewModel.CorrectedMapSizeX + pos.x].Connections = DirectionsAsByte.NOT_WALKABLE;

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

    private void VerifyAllChunks(Cell[] map, Chunk[] chunks, string config)
    {
        // Directed abstract graph of the whole map, in global portal-key space:
        // internal edges (both directions) plus every valid external edge.
        var graphEdges = new List<(int From, int To)>();
        var externalEdges = new List<(int From, int To)>();

        foreach (var chunk in chunks)
        {
            var portals = new List<(byte Key, Portal Portal)>();
            for (byte key = 0; key < MainWindowViewModel.MaxPortalsInChunk; key++)
            {
                var portal = chunk.portals[key];
                if (portal is null) continue;
                if (portal.CenterPos is null)
                {
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} has no CenterPos");
                    continue;
                }

                portals.Add((key, portal));
            }

            var reachability = new Dictionary<byte, bool[]>();
            foreach (var (key, portal) in portals)
                reachability[key] = FloodFillInsideChunk(map, portal.CenterPos);

            foreach (var (key1, portal1) in portals)
            {
                int globalKey1 = chunk.ChunkId * MainWindowViewModel.MaxPortalsInChunk + key1;
                var connections = new Dictionary<byte, ushort>();
                for (int i = 0; i < portal1.InternalPortalCount; i++)
                {
                    var connection = portal1.InternalPortalConnections[i];
                    if (!connections.TryAdd(connection.portalKey, connection.cost))
                        AddFailure($"{config}: chunk {chunk.ChunkId} portal {key1}: " +
                                   $"duplicate connection to portal {connection.portalKey}");
                    if (chunk.portals[connection.portalKey] is null)
                        AddFailure($"{config}: chunk {chunk.ChunkId} portal {key1}: " +
                                   $"internal connection to missing portal slot {connection.portalKey}");
                    else
                        graphEdges.Add((globalKey1, chunk.ChunkId * MainWindowViewModel.MaxPortalsInChunk + connection.portalKey));
                }

                int region1 = chunk.regions[RegionUtils.PositionToRegionKey(portal1.CenterPos)];
                foreach (var (key2, portal2) in portals)
                {
                    if (key1 == key2) continue;

                    bool reachable = reachability[key1][RegionUtils.PositionToRegionKey(portal2.CenterPos)];
                    bool connected = connections.TryGetValue(key2, out ushort cost);

                    if (connected != reachable)
                        AddFailure($"{config}: chunk {chunk.ChunkId}: portal {key1} {portal1.CenterPos} " +
                                   $"{(reachable ? "should connect to" : "must not connect to")} " +
                                   $"portal {key2} {portal2.CenterPos}, but the connection " +
                                   $"{(connected ? "exists" : "is missing")}");

                    int region2 = chunk.regions[RegionUtils.PositionToRegionKey(portal2.CenterPos)];
                    if ((region1 == region2) != reachable)
                        AddFailure($"{config}: chunk {chunk.ChunkId}: portals {key1} {portal1.CenterPos} and " +
                                   $"{key2} {portal2.CenterPos}: same region={region1 == region2}, reachable={reachable}");

                    if (!connected) continue;
                    if (!TryGetConnectionCost(portal2, key1, out ushort reverseCost))
                        AddFailure($"{config}: chunk {chunk.ChunkId}: portal {key1} connects to portal {key2}, " +
                                   $"but portal {key2} has no reverse connection");
                    else if (reverseCost != cost)
                        AddFailure($"{config}: chunk {chunk.ChunkId}: connection {key1} -> {key2} costs {cost}, " +
                                   $"reverse connection costs {reverseCost}");
                }
            }

            VerifyExternalConnections(chunk, chunks, portals, config, graphEdges, externalEdges);
        }

        VerifyExternalReturnPaths(chunks, graphEdges, externalEdges, config);
    }

    /// <summary>
    /// Every external connection must point at an existing portal in an adjacent chunk,
    /// must not be duplicated, and every portal must have at least one of them.
    /// Strict pairwise mirroring is only counted: diagonal corner portals replace
    /// straight corner portals, so the mirror is not guaranteed by construction.
    /// </summary>
    private void VerifyExternalConnections(Chunk chunk, Chunk[] chunks, List<(byte Key, Portal Portal)> portals,
        string config, List<(int From, int To)> graphEdges, List<(int From, int To)> externalEdges)
    {
        foreach (var (key, portal) in portals)
        {
            int ownGlobalKey = chunk.ChunkId * MainWindowViewModel.MaxPortalsInChunk + key;
            if (portal.ExternalPortalCount == 0)
                AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos} " +
                           "has no external connections");

            var seenExternalKeys = new HashSet<int>();
            for (int i = 0; i < portal.ExternalPortalCount; i++)
            {
                int externalKey = portal.ExternalPortalConnections[i];
                if (!seenExternalKeys.Add(externalKey))
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos}: " +
                               $"duplicate external connection to {externalKey}");

                if (externalKey < 0)
                {
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos}: " +
                               $"external connection key {externalKey} is negative");
                    continue;
                }

                int targetChunkId = externalKey / MainWindowViewModel.MaxPortalsInChunk;
                int targetSlot = externalKey % MainWindowViewModel.MaxPortalsInChunk;
                if (targetChunkId >= chunks.Length)
                {
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos}: " +
                               $"external connection {externalKey} points outside the chunk grid");
                    continue;
                }

                int chunkX = chunk.ChunkId % MainWindowViewModel.ChunkMapSizeX;
                int chunkY = chunk.ChunkId / MainWindowViewModel.ChunkMapSizeX;
                int targetX = targetChunkId % MainWindowViewModel.ChunkMapSizeX;
                int targetY = targetChunkId / MainWindowViewModel.ChunkMapSizeX;
                if (targetChunkId == chunk.ChunkId || Math.Abs(targetX - chunkX) > 1 || Math.Abs(targetY - chunkY) > 1)
                {
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos}: " +
                               $"external connection {externalKey} points to non-adjacent chunk {targetChunkId}");
                    continue;
                }

                var targetPortal = chunks[targetChunkId].portals[targetSlot];
                if (targetPortal is null)
                {
                    AddFailure($"{config}: chunk {chunk.ChunkId} portal {key} {portal.CenterPos}: " +
                               $"external connection {externalKey} points to missing portal " +
                               $"(chunk {targetChunkId} slot {targetSlot})");
                    continue;
                }

                graphEdges.Add((ownGlobalKey, externalKey));
                externalEdges.Add((ownGlobalKey, externalKey));

                bool mirrored = false;
                for (int j = 0; j < targetPortal.ExternalPortalCount; j++)
                    if (targetPortal.ExternalPortalConnections[j] == ownGlobalKey)
                    {
                        mirrored = true;
                        break;
                    }

                if (!mirrored)
                    _unmirroredExternalEdgeCount++;
            }
        }
    }

    /// <summary>
    /// Every external edge must have a return path through the directed abstract
    /// graph; otherwise HPA* cannot travel the border crossing in both directions
    /// and loses paths. Checked by comparing strongly connected components.
    /// </summary>
    private void VerifyExternalReturnPaths(Chunk[] chunks, List<(int From, int To)> graphEdges,
        List<(int From, int To)> externalEdges, string config)
    {
        int nodeCount = chunks.Length * MainWindowViewModel.MaxPortalsInChunk;
        var scc = TarjanStronglyConnectedComponents(nodeCount, graphEdges);
        foreach (var (from, to) in externalEdges)
        {
            if (scc[from] == scc[to]) continue;
            AddFailure($"{config}: external connection {DescribePortal(chunks, from)} -> " +
                       $"{DescribePortal(chunks, to)} has no return path through the abstract graph");
        }
    }

    private static string DescribePortal(Chunk[] chunks, int globalKey)
    {
        int chunkId = globalKey / MainWindowViewModel.MaxPortalsInChunk;
        int slot = globalKey % MainWindowViewModel.MaxPortalsInChunk;
        return $"chunk {chunkId} portal {slot} {chunks[chunkId].portals[slot]?.CenterPos}";
    }

    /// <summary>
    /// Tarjan's algorithm; returns the strongly connected component id per node.
    /// </summary>
    private static int[] TarjanStronglyConnectedComponents(int nodeCount, List<(int From, int To)> edges)
    {
        var adjacency = new List<int>[nodeCount];
        foreach (var (from, to) in edges)
            (adjacency[from] ??= []).Add(to);

        var index = new int[nodeCount];
        var lowLink = new int[nodeCount];
        var onStack = new bool[nodeCount];
        var scc = new int[nodeCount];
        Array.Fill(index, -1);
        var stack = new Stack<int>();
        int nextIndex = 0;
        int sccCount = 0;

        for (int node = 0; node < nodeCount; node++)
            if (index[node] == -1)
                StrongConnect(node);

        return scc;

        void StrongConnect(int v)
        {
            index[v] = lowLink[v] = nextIndex++;
            stack.Push(v);
            onStack[v] = true;
            if (adjacency[v] is not null)
                foreach (var w in adjacency[v])
                {
                    if (index[w] == -1)
                    {
                        StrongConnect(w);
                        lowLink[v] = Math.Min(lowLink[v], lowLink[w]);
                    }
                    else if (onStack[w])
                    {
                        lowLink[v] = Math.Min(lowLink[v], index[w]);
                    }
                }

            if (lowLink[v] != index[v]) return;
            sccCount++;
            while (true)
            {
                int w = stack.Pop();
                onStack[w] = false;
                scc[w] = sccCount;
                if (w == v) break;
            }
        }
    }

    private static bool TryGetConnectionCost(Portal portal, byte otherPortalKey, out ushort cost)
    {
        for (int i = 0; i < portal.InternalPortalCount; i++)
        {
            var connection = portal.InternalPortalConnections[i];
            if (connection.portalKey != otherPortalKey) continue;
            cost = connection.cost;
            return true;
        }

        cost = 0;
        return false;
    }

    /// <summary>
    /// Independent ground truth: flood fill from the start cell, confined to the chunk,
    /// moving through the 8 direction bits of each cell (bit set = blocked), exactly the
    /// movement model the portal builder and BFS use.
    /// </summary>
    private static bool[] FloodFillInsideChunk(Cell[] map, Vector2D start)
    {
        var visited = new bool[MainWindowViewModel.CellsInChunk];
        var open = new Queue<Vector2D>();
        open.Enqueue(start);
        visited[RegionUtils.PositionToRegionKey(start)] = true;
        while (open.Count > 0)
        {
            var current = open.Dequeue();
            var cell = map[current.y * MainWindowViewModel.CorrectedMapSizeX + current.x];
            int localX = current.x % ChunkSize;
            int localY = current.y % ChunkSize;
            for (int i = 0; i < DirectionsVector.AllDirections.Length; i++)
            {
                if ((cell.Connections & DirectionsAsByte.AllDirectionsAsByte[i]) != DirectionsAsByte.WALKABLE)
                    continue;

                var dir = DirectionsVector.AllDirections[i];
                int newLocalX = localX + dir.x;
                int newLocalY = localY + dir.y;
                if (newLocalX < 0 || newLocalX >= ChunkSize || newLocalY < 0 || newLocalY >= ChunkSize)
                    continue;

                int key = newLocalY * ChunkSize + newLocalX;
                if (visited[key]) continue;
                visited[key] = true;
                open.Enqueue(current + dir);
            }
        }

        return visited;
    }

    private void AddFailure(string message)
    {
        _failureCount++;
        if (_failureMessages.Count < MaxStoredFailureMessages)
            _failureMessages.Add(message);
    }
}
