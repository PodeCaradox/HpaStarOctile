using Xunit;

// All tests in this assembly share mutable static state (MainWindowViewModel map
// dimensions, PortalUtils offset tables, Chunk.ChunkIdCounter), so they must not
// run in parallel — otherwise one class rebuilds the statics while another is
// mid-scenario, producing wrong chunk ids and out-of-range map access.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
