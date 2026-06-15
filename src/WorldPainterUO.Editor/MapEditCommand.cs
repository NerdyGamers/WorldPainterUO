using WorldPainterUO.Core;

namespace WorldPainterUO.Editor;

public sealed class MapEditCommand : ICommand
{
    public string Description { get; }
    private readonly ChunkDiff[] _diffs;
    private readonly bool _modifiesTerrain;
    private readonly bool _modifiesHeight;

    internal MapEditCommand(string description, ChunkDiff[] diffs, bool modifiesTerrain, bool modifiesHeight)
    {
        Description = description;
        _diffs = diffs;
        _modifiesTerrain = modifiesTerrain;
        _modifiesHeight = modifiesHeight;
    }

    public bool Execute(WorldMap map) => Apply(map, static d => d.After);

    public bool Undo(WorldMap map) => Apply(map, static d => d.Before);

    private bool Apply(WorldMap map, Func<ChunkDiff, MapTile[]> source)
    {
        if (_diffs.Length == 0)
            return false;

        foreach (var diff in _diffs)
        {
            var terrainChunk = map.Terrain.GetChunk(diff.ChunkX, diff.ChunkY);
            var heightChunk = map.Height.GetChunk(diff.ChunkX, diff.ChunkY);

            for (var i = 0; i < diff.Before.Length; i++)
            {
                var tile = source(diff)[i];

                if (_modifiesTerrain)
                    terrainChunk[i % 64, i / 64] = tile.LandTileId;

                if (_modifiesHeight)
                    heightChunk[i % 64, i / 64] = tile.Z;
            }
        }

        return true;
    }

    internal static MapEditCommand Create(
        string description,
        WorldMap map,
        IEnumerable<(int X, int Y)> tiles,
        Func<ushort, sbyte, (ushort TileId, sbyte Z)> modifier,
        bool modifiesTerrain = true,
        bool modifiesHeight = true)
    {
        // Group tiles by their parent chunk
        var grouped = new Dictionary<(int, int), List<(int X, int Y)>>();

        foreach (var (tx, ty) in tiles)
        {
            if (tx < 0 || ty < 0 || tx >= map.Dimensions.Width || ty >= map.Dimensions.Height)
                continue;

            map.Dimensions.GetChunkCoord(tx, ty, out var cx, out var cy, out _, out _);
            var key = (cx, cy);

            if (!grouped.TryGetValue(key, out var list))
            {
                list = [];
                grouped[key] = list;
            }

            list.Add((tx, ty));
        }

        if (grouped.Count == 0)
            return new MapEditCommand(description, [], modifiesTerrain, modifiesHeight);

        var chunks = map.Dimensions;
        var size = MapChunk<ushort>.Size;
        var count = MapChunk<ushort>.TileCount;
        var diffs = new List<ChunkDiff>();

        foreach (var ((cx, cy), tileList) in grouped)
        {
            var terrainChunk = map.Terrain.GetChunk(cx, cy);
            var heightChunk = map.Height.GetChunk(cx, cy);

            // Capture full before state for this chunk
            var before = new MapTile[count];
            for (var i = 0; i < count; i++)
                before[i] = new MapTile(terrainChunk[i % size, i / size], heightChunk[i % size, i / size]);

            var after = new MapTile[count];
            Array.Copy(before, after, count);
            var changed = false;

            foreach (var (tx, ty) in tileList)
            {
                chunks.GetChunkCoord(tx, ty, out _, out _, out var lx, out var ly);
                var idx = ly * size + lx;
                var origTileId = terrainChunk[lx, ly];
                var origZ = heightChunk[lx, ly];
                var (newTileId, newZ) = modifier(origTileId, origZ);

                var tileChanged = modifiesTerrain && newTileId != origTileId;
                var zChanged = modifiesHeight && newZ != origZ;

                if (!tileChanged && !zChanged)
                    continue;

                after[idx] = new MapTile(
                    modifiesTerrain ? newTileId : origTileId,
                    modifiesHeight ? newZ : origZ);
                changed = true;
            }

            if (changed)
                diffs.Add(new ChunkDiff(cx, cy, before, after));
        }

        return new MapEditCommand(description, diffs.ToArray(), modifiesTerrain, modifiesHeight);
    }
}
