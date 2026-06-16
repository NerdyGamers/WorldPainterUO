# TASKS.md

This file is updated by Codex at the end of every milestone pass.

## Milestone 1 — Solution skeleton
- [x] Create .NET 8 solution
- [x] Create `WorldPainterUO.Core` project
- [x] Create `WorldPainterUO.FileFormats` project
- [x] Create `WorldPainterUO.Project` project
- [x] Create `WorldPainterUO.Rendering` project
- [x] Create `WorldPainterUO.Editor` project
- [x] Create `WorldPainterUO.App` Avalonia project
- [x] Create `WorldPainterUO.Tests` xUnit project
- [x] Wire all project references
- [x] CI build/test workflow (pre-existing)
- [x] Verify build passes

## Milestone 2 — Core domain model
- [x] Define `MapTile` record
- [x] Define `MapChunk<T>` (generic, 64x64, dirty flag, index)
- [x] Define `MapDimensions` and `MapBounds`
- [x] Define `MapMetadata` and `SourceFileType` enum
- [x] Define `TerrainLayer` abstraction (`MapChunk<ushort>`)
- [x] Define `HeightLayer` abstraction (`MapChunk<sbyte>`)
- [x] Implement dirty-chunk tracking (per-chunk `IsDirty` + `DirtyChunks` enumerable)
- [x] Add unit tests (40 tests across 8 test classes)

## Milestone 3 — MUL reader
- [x] Implement `UltimaMapReader` (implements `IMapFileReader`, wraps ported `TileMatrix`)
- [x] Parse tile IDs (uint16 LE) and Z values (int8)
- [x] Support `map0` through `map5` (shared format, dimensions vary)
- [x] Add synthetic golden-file tests (12 tests across 8x8, 16x16, 20x20, 64x64)
- [x] Add Britannia-scale smoke test (6144×4096, ~25M tiles, reads in <1s)
- [x] Port `Ultima/TileMatrix.cs`, `Ultima/Tile.cs`, `Ultima/UopUtils.cs` from UOFiddler source
- [x] Removed old `MulMapReader.cs` (replaced by `UltimaMapReader`)

## Milestone 4 — Project format
- [x] Define `.uomap` JSON schema (per-chunk base64 binary data)
- [x] Implement `UomapSerializer.Save` (chunked terrain + height serialization)
- [x] Implement `UomapSerializer.Load` (deserialize + reconstruct WorldMap)
- [x] Add `UomapAutosave` (write-then-rename snapshot, load, delete, exists)
- [x] Add save/load tests (14 new tests: round-trip, metadata, autosave, error cases)

## Milestone 5 — MUL writer
- [x] Implement `UltimaMapWriter` (implements `IMapFileWriter`)
- [x] Export valid `mapX.mul` with correct byte layout (196-byte blocks: 4-byte header + 192-byte tile data)
- [x] Add round-trip tests (import → export → re-import, 8×8 through 64×64, non-aligned)
- [x] Add `MapExportValidator` with tile-ID range, dimension checks, and `IsExportable` helper
- [x] Removed old `MulMapWriter.cs` (replaced by `UltimaMapWriter`)

## Milestone 6 — UOP support
- [x] Add UOP container abstraction (`UopFormat`, `UopFormatException`)
- [x] Import `map0LegacyMUL.uop` via `UltimaMapReader` (per-block UOP via TileMatrix, fallback to legacy single-entry)
- [x] Export `map0LegacyMUL.uop` via `UopMapWriter` (implements `IMapFileWriter`)
- [x] Add round-trip tests (12 new: 8×8, 16×16, 64×64, 20×20, MUL→UOP→UOP, offsets, errors)
- [x] Removed old `UopMapReader.cs` (replaced by `UltimaMapReader`)

## Milestone 7 — Avalonia shell
- [x] Create main window (File menu, right panel, viewport area, status bar)
- [x] Add viewport host (render-to-bitmap via SkiaSharp, 50ms render loop)
- [x] Add zoom/pan (mouse wheel zoom toward cursor, click-drag pan)
- [x] Add status bar (tile X/Y, tile ID, Z value, zoom %)
- [x] Add layer switcher (Terrain/Height toggles, wired to VM)
- [x] Add tile inspection readout (cursor tracking on viewport)
- [x] Add minimap stub (placeholder panel in right sidebar)
- [x] New Map dialog (width/height/facet input)
- [x] Open .mul/.uop file dialog (storage provider API)

## Milestone 8 — Editing tools
- [x] ICommand interface with chunk-level before/after snapshots
- [x] CommandHistory (undo/redo stack, capacity cap, clear, StateChanged event)
- [x] BrushShapes helper (circle, square tile enumerators)
- [x] MapEditCommand.Create — generic diff capture from tile modifier lambda
- [x] PaintBrushTool — paints terrain tile IDs, configurable radius/opacity/randomization
- [x] FillTool — flood-fill connected tiles by tile ID (4-directional, bounds-safe)
- [x] ReplaceTool — replace all matching tile IDs across full map or bounds
- [x] RaiseTool — increase Z under brush, clamped to sbyte range
- [x] LowerTool — decrease Z under brush, clamped to sbyte range
- [x] SmoothTool — average Z of 3×3 neighbor neighborhood (pre-edit snapshot, no cascading)
- [x] FlattenTool — set Z to target value under brush
- [x] NoiseTool — random Z jitter with configurable range and deterministic seed
- [x] Chunk dirty flags set correctly on edit (terrain-only vs height-only)
- [x] 29 unit tests: undo/redo round-trips, clamp behavior, flood-fill bounds, multi-step chain, redo-stack clear, dirty flags
- [x] All 127 tests pass (98 existing + 29 new)

## Milestone 9 — Rendering
- [x] ViewMode enum (Radar, Terrain, Hybrid)
- [x] ITileTextureProvider interface (abstraction for tile visuals)
- [x] FallbackTileTextureProvider (procedural terrain fallback when art files missing)
- [x] RadarColorPalette (stable tile-ID → color mapping, 32 base groups)
- [x] RenderCache (chunk-level SKBitmap cache with dirty invalidation, SyncDirtyChunks)
- [x] OverlayRenderer (tile grid + chunk grid, configurable colors)
- [x] MinimapRenderer (full-map reduced resolution, cached, Invalidate())
- [x] MapRenderService (orchestrator: view mode switching, zoom/pan, chunk render, dirty sync, three view modes)
- [x] Radar view — radar-color rendering with height shading
- [x] Terrain view — procedural terrain fallback (shaded radar + internal pattern)
- [x] Hybrid view — terrain fallback with radar-color tint overlay
- [x] Removed old MapRenderer.cs (replaced by MapRenderService)
- [x] MainWindow uses MapRenderService with dirty-chunk sync before each render
- [x] 25 new rendering tests (cache invalidation, view mode switching, all view modes render, minimap caching, overlay smoke tests)
- [x] All 152 tests pass (127 existing + 25 new)

## Milestone 10 — Terrain rules
- [x] WeightedTileEntry record (tile ID + float weight, JSON-serializable)
- [x] BiomeDefinition model (named biome with tile list, neighbor transition overrides, validation)
- [x] TerrainPalette (dictionary of biomes, default palette with 11 UO biomes, JSON save/load)
- [x] TileClassifier (auto-group 0x4000 tiles into biomes using tiledata.mul flags + radarcol.mul colors, range-based fallback)
- [x] tiledata.mul reader (512 groups × 32 entries, flags + name parsing)
- [x] radarcol.mul reader (BGRA color array for all land tiles)
- [x] TerrainRulesEngine (weighted random selection, neighbor-aware transitions, deterministic seed)
- [x] Neighbor-aware transitions: detect adjacent biome boundaries, select blend tiles from transition overrides
- [x] Deterministic seed: same seed produces identical tile ID sequence
- [x] Biome definitions overridable via JSON without recompilation
- [x] No changes to MapTile or TerrainLayer data model
- [x] 32 new unit tests (palette loading, weighted selection distribution, transition logic, tile classification, save/load round-trip, file-not-found handling)
- [x] All 184 tests pass (152 existing + 32 new)

## Milestone 11 — Export validation
- [x] Bounds checks
- [x] Height range checks
- [x] Invalid tile checks
- [x] Export diagnostics UI
- [x] MapExportValidator with dimension, tile-range, height-range, and chunk integrity checks
- [x] ValidationResult with per-chunk diagnostic messages
- [x] Export diagnostics: IsExportable helper, 22 validation tests

## Milestone 12 — Procedural generation, selection, stamp tool, release
- [x] BiomeStyle enum (Temperate, Desert, Tropical, Arctic, Volcanic, Mixed)
- [x] IMapGenerator interface
- [x] NoiseHelper with multi-octave value noise
- [x] GenerateIsland — single island with configurable seed, size, biome style
- [x] GenerateArchipelago — multiple islands, configurable count via density
- [x] GenerateContinent — large landmass with interior biome variation
- [x] GenerateWorld — full map combining continent + archipelago + ocean fill
- [x] All generators produce valid terrain within tile ID range (0–0x3FFF)
- [x] Deterministic seed: same seed produces identical output
- [x] Different seeds and biome styles produce distinct outputs
- [x] ISelection interface with Contains(x,y) and Bounds
- [x] RectangleSelection — normalized inclusive bounds
- [x] LassoSelection — freehand polygon with ray-cast containment
- [x] Apply-to-selection mode on all 8 existing editing tools (PaintBrush, Fill, Replace, Raise, Lower, Smooth, Flatten, Noise)
- [x] StampTemplate model with JSON serialization (tile IDs + heights, width, height, name, description)
- [x] StampTemplateManager loads templates from directory
- [x] StampTool — places template at cursor with rotation (0°, 90°, 180°, 270°)
- [x] StampTool respects selection and map bounds
- [x] 3 bundled stamp templates: mountain_range.json, lake.json, forest_cluster.json
- [x] 27 new unit tests (generators, selection, stamp tool, stamp template save/load)
- [x] Self-contained release publish script (win-x64, linux-x64, osx-x64)
- [x] GitHub Actions release workflow triggered by version tags
- [x] CHANGELOG.md, publish script, release workflow, README updates
- [x] All 211 tests pass (184 existing + 27 new)

## Milestone 13 — UI polish
- [x] ZoomIn/ZoomOut centered on viewport center (ZoomAtPoint with viewport center coords)
- [x] Mouse-wheel zoom toward cursor (fixed HandleScroll removed spurious `* ratio`)
- [x] Export .mul/.uop via File → Export menu (UltimaMapWriter / UopMapWriter)
- [x] Project save/load (.uomap serialization, FilePath/IsDirty, title bar with filename + dirty indicator)
- [x] Brush preview circle on viewport (drawn via SkiaSharp overlay)
- [x] Grid/render-mode checkmarks (IsChecked bindings on View menu)
- [x] Replace tool button in toolbar + keyboard shortcut (E)
- [x] Zoom step refined (1.2× instead of 2× for smoother zooming)
- [x] Status bar segments (tool name, X/Y, ID/hex, Z, zoom%/blue)
- [x] Brush radius/strength/hardness numeric readouts (third column in brush grid)
- [x] Minimap enlarged (200×160 → 224×240 to fill right panel)
- [x] Unsaved changes confirmation on close (Yes/No/Cancel dialog)
- [x] MessageBox extended with button enums (MessageBoxButtons/MessageBoxResult)
- [x] Autosave writes files (TriggerAutosave → UomapAutosave.SaveSnapshot)
- [x] Drag-and-drop file opening (IDataTransfer.TryGetFiles, DragEnter/Drop handlers)
- [x] Selection visual feedback (RectSelect/LassoSelect overlay drawing + mouse handlers)
- [x] Open dialog/recent files support .uomap (file filter routing)
- [x] LassoSelection.Polygon public accessor for overlay rendering
- [x] Clear Selection (Escape key clears ActiveSelection)
- [x] Zoom slider in right panel
- [x] All 211 tests pass
