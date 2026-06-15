# WorldPainterUO Implementation Plan

## Overview

Build round-trip map fidelity first, then editing, then preview and performance, then procedural tools and extensibility. Format correctness before UI ambition.

## Phase 0 — Scope and research

- Lock V1 to land-map editing only.
- Confirm supported map sizes and source file samples.
- Collect golden files for regression tests.
- Finalize solution structure and milestone sequence.

**Deliverables:** Confirmed solution structure. Technical decisions documented. Golden sample map files available for tests.

## Phase 1 — Repository and solution scaffold

- Create the .NET 8 solution.
- Create all target projects and wire references.
- Add test project and CI workflow.
- Establish coding conventions.

**Exit criteria:** Solution builds. CI passes. Docs exist.

## Phase 2 — Core domain model

- Define map tile, chunk, bounds, and metadata.
- Define terrain and height layer abstractions.
- Implement dirty-chunk tracking.
- Add unit tests for invariants.

**Exit criteria:** Domain model is clean, tested, and independent of file formats.

## Phase 3 — MUL reader

- Implement block-based map reader.
- Parse tile IDs and Z values.
- Support `map0` through `map5`.
- Add golden-file and smoke tests.

**Exit criteria:** Import a map. Values match source. Large-map smoke test passes.

## Phase 4 — Project format

- Implement `.uomap` JSON schema.
- Save/load terrain, height, and metadata.
- Add autosave snapshot support.
- Add serialization tests.

**Exit criteria:** Open an imported map, save as `.uomap`, reopen with no data loss.

## Phase 5 — MUL writer

- Implement `mapX.mul` block encoder.
- Add round-trip tests (import → export → import).
- Add export validation errors.

**Exit criteria:** Export produces logically equivalent map data to the source.

## Phase 6 — UOP support

- Add UOP container abstraction.
- Import `map0LegacyMUL.uop`.
- Export `map0LegacyMUL.uop`.
- Add compatibility and round-trip tests.

**Exit criteria:** UOP import and export work. Round-trip is logically equivalent.

## Phase 7 — Avalonia shell

- Build desktop shell.
- Add viewport host with zoom and pan.
- Add status bar, layer switcher, map inspection readout.
- Add minimap stub.

**Exit criteria:** User can open and navigate a large map.

## Phase 8 — Editing tools

- Add terrain paint brush.
- Add raise, lower, smooth, flatten height tools.
- Add fill and replace tools.
- Add undo/redo command stack.
- Add command tests.

**Exit criteria:** Unlimited undo/redo works across mixed edits. Edited map exports correctly.

## Phase 9 — Rendering and overlays

- Radar view.
- Terrain and hybrid preview.
- Grid, region, and selection overlays.
- Optimized chunk redraw.

**Exit criteria:** View modes switch instantly. Large maps stay responsive.

## Phase 10 — Terrain rules engine

- Terrain groups from palette source.
- Weighted tile mapping.
- Neighbor-aware transitions.
- Deterministic seed option.

**Exit criteria:** Biome painting resolves to legal tiles. Results are reproducible.

## Phase 11 — Export validation and workflow verification

- Validate heights, tile ranges, and export structure.
- Add diagnostics UI.
- Verify expected ServUO workflow compatibility.

**Exit criteria:** Exported maps load in a ServUO test environment without manual repair.

## Phase 12 — Polish and release prep

- Autosave recovery UX.
- Preferences.
- Logging and error handling.
- Packaging and contributor docs.
