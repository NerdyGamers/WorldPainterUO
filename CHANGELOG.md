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
- GitHub Actions CI and release workflows
- Cross-platform publish scripts (win-x64, linux-x64, osx-x64)
