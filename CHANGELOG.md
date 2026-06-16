# Changelog

All notable changes to WorldPainterUO are documented in this file.

## [1.0.0] — Unreleased

### Added
- Project skeleton with Avalonia UI shell
- Core domain model: MapTile, MapChunk<T>, MapDimensions, MapBounds, TerrainLayer, HeightLayer, WorldMap
- MUL map file reader (UltimaMapReader) with TileMatrix port from UOFiddler
- MUL map file writer (UltimaMapWriter) with round-trip export validation
- UOP container support (map0LegacyMUL.uop) — read and write
- JSON-based .uomap project format with chunked Base64 serialization
- Autosave with write-then-rename snapshot strategy
- Avalonia main window with viewport, zoom/pan, status bar, layer switcher, minimap
- Editing tools: PaintBrush, Fill, Replace, Raise, Lower, Smooth, Flatten, Noise
- Undo/redo system with CommandHistory and chunk-level diffs
- SkiaSharp rendering with Radar, Terrain, and Hybrid view modes
- Chunk-level render cache with dirty invalidation
- Radar color palette and fallback tile textures
- Overlay renderer (tile grid + chunk grid)
- Minimap renderer with full-map cached output
- Terrain rules engine with biome definitions, weighted tile selection, neighbor transitions
- Terrain palette with 11 biomes (Ocean, Grass, Forest, Swamp, Snow, Desert, Mountain, Volcanic, Marsh, Road, Rock)
- Tile classifier with tiledata.mul and radarcol.mul reading
- Export validator with dimension, tile range, height range, and chunk integrity checks
- **Procedural map generators**: GenerateIsland, GenerateArchipelago, GenerateContinent, GenerateWorld
- **Selection tools**: RectangleSelection, LassoSelection with apply-to-selection mode
- **Stamp tool**: load and place terrain templates from JSON files with rotation support
- **Stamp templates**: mountain range, lake, forest cluster
- File menu: Save/Save As/Open/Export with .uomap project support
- Brush preview circle on viewport when brush tool is active
- Grid and render-mode checkmarks on View menu
- Replace tool button on toolbar
- Recent files list with persistence
- Title bar shows filename, dirty indicator, and map dimensions
- View menu: Zoom In/Out/Reset with keyboard shortcuts
- GitHub Actions CI and release workflows
- Cross-platform publish scripts (win-x64, linux-x64, osx-x64)

### Fixed
- UOP writer header layout: nextBlock at offset 12–19 (was 8–15)
- UOP writer pattern derivation: uses filename basename (case-insensitive) instead of hardcoded map index
- UOP writer block ordering: column-major (x outer, y inner) matching SDK's MUL offset formula
- MUL/UOP writer block count: floor division (`>> 3`) instead of ceiling, matching SDK TileMatrix
- Static state leakage in UltimaMapReader: reset stale MulPath entries before registering new path
- Input validation: null/empty/file-exists/size checks on MUL reader open
- Zoom centering: removed spurious `* ratio` in HandleScroll formula
- ZoomIn/ZoomOut center on viewport center instead of top-left
- Zoom step: changed from 2x to 1.2x for finer keyboard zoom control
- Parallel test races: collection-based serialization for SDK-dependent test classes
- Open dialog and recent files now support .uomap files
