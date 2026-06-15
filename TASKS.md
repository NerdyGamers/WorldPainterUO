# TASKS.md

This file is updated by Codex at the end of every milestone pass.

## Milestone 1 — Solution skeleton
- [ ] Create .NET 8 solution
- [ ] Create `WorldPainterUO.Core` project
- [ ] Create `WorldPainterUO.FileFormats` project
- [ ] Create `WorldPainterUO.Project` project
- [ ] Create `WorldPainterUO.Rendering` project
- [ ] Create `WorldPainterUO.Editor` project
- [ ] Create `WorldPainterUO.App` Avalonia project
- [ ] Create `WorldPainterUO.Tests` xUnit project
- [ ] Wire all project references
- [ ] Add CI build/test workflow
- [ ] Verify build passes
- [ ] Add README run instructions

## Milestone 2 — Core domain model
- [ ] Define `MapTile` record
- [ ] Define `MapChunk`
- [ ] Define `MapDimensions` and `MapBounds`
- [ ] Define `MapMetadata`
- [ ] Define `TerrainLayer` abstraction
- [ ] Define `HeightLayer` abstraction
- [ ] Implement dirty-chunk tracking
- [ ] Add unit tests

## Milestone 3 — MUL reader
- [ ] Implement `MulMapReader`
- [ ] Parse tile IDs
- [ ] Parse Z values
- [ ] Support `map0` through `map5`
- [ ] Add golden-file tests
- [ ] Add large-map smoke test

## Milestone 4 — Project format
- [ ] Define `.uomap` JSON schema
- [ ] Implement project serializer
- [ ] Implement project deserializer
- [ ] Add autosave snapshot model
- [ ] Add save/load tests

## Milestone 5 — MUL writer
- [ ] Implement `MulMapWriter`
- [ ] Export valid `mapX.mul`
- [ ] Add round-trip tests
- [ ] Add validation errors

## Milestone 6 — UOP support
- [ ] Add UOP container abstraction
- [ ] Import `map0LegacyMUL.uop`
- [ ] Export `map0LegacyMUL.uop`
- [ ] Add compatibility tests

## Milestone 7 — Avalonia shell
- [ ] Create main window
- [ ] Add viewport host
- [ ] Add zoom/pan
- [ ] Add status bar
- [ ] Add layer switcher
- [ ] Add tile inspection readout
- [ ] Add minimap stub

## Milestone 8 — Editing tools
- [ ] Terrain paint brush
- [ ] Raise/lower height
- [ ] Smooth height
- [ ] Flatten height
- [ ] Fill tool
- [ ] Replace tool
- [ ] Undo/redo stack
- [ ] Command tests

## Milestone 9 — Rendering
- [ ] Radar view
- [ ] Terrain view
- [ ] Hybrid view
- [ ] Chunk redraw cache
- [ ] Grid overlay
- [ ] Selection overlay
- [ ] Minimap

## Milestone 10 — Terrain rules
- [ ] Palette definitions
- [ ] Weighted tile mapping
- [ ] Neighbor-aware transitions
- [ ] Deterministic seed option

## Milestone 11 — Export validation
- [ ] Bounds checks
- [ ] Height range checks
- [ ] Invalid tile checks
- [ ] Export diagnostics UI

## Milestone 12 — Polish
- [ ] Autosave recovery UX
- [ ] Preferences
- [ ] Logging
- [ ] Error handling
- [ ] Packaging docs
