# WorldPainterUO Architecture

## Architecture goals

- Preserve UO land-map fidelity.
- Separate core domain logic from UI.
- Support large maps through chunking.
- Keep file-format logic testable and isolated.
- Enable milestone delivery without architectural rewrites.

## Solution layout

```text
src/
  WorldPainterUO.Core/
  WorldPainterUO.FileFormats/
  WorldPainterUO.Project/
  WorldPainterUO.Rendering/
  WorldPainterUO.Editor/
  WorldPainterUO.App/
tests/
  WorldPainterUO.Tests/
```

## Module responsibilities

### WorldPainterUO.Core

Domain model only. No IO, no UI.

- `MapTile` — `ushort LandTileId`, `sbyte Z`
- `MapChunk` — fixed tile array, dirty flag
- `MapDimensions` — width, height, block layout
- `MapMetadata` — facet, source file type, version
- `TerrainLayer` — editable terrain intent per tile
- `HeightLayer` — editable elevation per tile
- Shared enums and interfaces

### WorldPainterUO.FileFormats

All external file format support.

- `MulMapReader` — block-based tile/Z parsing
- `MulMapWriter` — block-based MUL encoder
- `UopMapReader` — UOP container unpacker for legacy map
- `UopMapWriter` — UOP container repacker
- Binary helpers and format diagnostics

### WorldPainterUO.Project

Non-destructive project persistence.

- `.uomap` JSON schema
- Save/load editable layer state
- Autosave snapshot support
- Project versioning and migration

### WorldPainterUO.Rendering

All visualization logic.

- Chunked viewport renderer (SkiaSharp)
- Radar view generator
- Terrain preview generator
- Minimap renderer
- Overlay renderer (grid, region, selection)
- Dirty-chunk redraw invalidation

### WorldPainterUO.Editor

All editing operations.

- `ICommand` pattern base
- Undo/redo stack
- Brush tools (terrain, height)
- Fill and replace tools
- Height tools (raise, lower, smooth, flatten)
- Selection state
- Terrain rules engine (biome-to-tile mapping)

### WorldPainterUO.App

Avalonia desktop shell.

- Shell window and menus
- MVVM view models
- Viewport host
- Toolbox panel
- Layer switcher
- Status bar and minimap
- Dialogs (import, export, new map)
- Preferences

### WorldPainterUO.Tests

- Parser unit tests
- Encoder unit tests
- Round-trip tests
- Project save/load tests
- Command undo/redo tests
- Integration workflow tests

## Internal data model

### MapTile

```csharp
public record struct MapTile
{
    public ushort LandTileId { get; init; }
    public sbyte Z { get; init; }
}
```

### Map organization

- Chunked into fixed-size regions for dirty tracking and partial redraw.
- Dirty flags are per-chunk, not per-tile.
- Terrain intent layer is separate from resolved tile IDs.
- Height values are signed bytes, range -128 to +127.

## Key design decisions

### Editable vs. exported state

Keep editable terrain intent (biome) separate from final resolved tile IDs. This allows biome-style painting without losing deterministic export behavior.

### Undo model

Command-pattern operations store chunk-level diffs. Full-map snapshots are not used for undo.

### Performance

Background chunk loading, cached render output, and partial dirty-chunk redraw instead of full-canvas re-render.

## Format notes

### MUL map format

- Maps are organized in blocks of 8x8 tiles.
- Each tile stores a 16-bit land tile ID and an 8-bit altitude.
- Map sizes vary by facet (e.g., Britannia: 6144x4096, Malas: 2560x2048).

### UOP legacy map format

- `map0LegacyMUL.uop` is a UOP container wrapping the MUL land-map data.
- UOP packaging adds a hash table and block offsets over raw MUL content.
- Treat UOP as a packaging layer over the same underlying land-map tiles.

## Risks to isolate early

- Exact UOP container structure details.
- Large-map memory pressure.
- Height editing edge cases and slope transitions.
- Renderer correctness versus performance tradeoffs.
