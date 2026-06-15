# PROMPTS.md

Copy/paste these prompts into Codex in order, one milestone at a time.

---

## Prompt 0 — Initial orientation (run this first, no code)

```
/plan

Build WorldPainterUO from the specs in this repository.

Read @AGENTS.md @SPEC.md @ARCHITECTURE.md @IMPLEMENTATION_PLAN.md @TASKS.md @TEST_PLAN.md first.

Task:
1. Review all repo docs and identify any missing decisions or contradictions.
2. List any clarifications you need before starting.
3. Propose a concrete implementation plan for Milestones 1, 2, and 3.
4. Do NOT write any code yet. Only plan.

Done when:
- You have confirmed understanding of the architecture.
- You have a clear plan for Milestones 1-3.
- Open questions are listed.
```

---

## Prompt 1 — Milestone 1: Solution skeleton

```
Implement Milestone 1 only.

Read @AGENTS.md @SPEC.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Create the .NET 8 solution.
- Create all projects listed in ARCHITECTURE.md under src/ and tests/.
- Wire project references as defined in ARCHITECTURE.md.
- Add xUnit test project (WorldPainterUO.Tests).
- Add .github/workflows/dotnet-ci.yml for build and test on push.
- Verify the solution builds cleanly.
- Update TASKS.md to mark completed items.
- Summarize what was created and what the next prompt should be.

Constraints:
- Use C# .NET 8.
- Use Avalonia for the App project.
- No placeholder implementations in FileFormats.
- Do not implement Milestone 2 yet.

Done when:
- Solution builds.
- CI config exists.
- TASKS.md is updated.
```

---

## Prompt 2 — Milestone 2: Core domain model

```
Implement Milestone 2 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Define MapTile record (ushort LandTileId, sbyte Z).
- Define MapChunk (tile array, dirty flag, bounds) using 64×64 tiles per chunk.
- Define MapDimensions and MapBounds.
- Define MapMetadata.
- Define TerrainLayer and HeightLayer abstractions storing raw ushort tile IDs.
- Implement dirty-chunk tracking.
- Add unit tests for all new types.
- Run build and tests.
- Update TASKS.md.

Constraints:
- Core project has no dependency on FileFormats, Rendering, or App.
- Models are immutable or clearly mutable with intent.
- No UI code.
- Chunk size is 64×64 tiles. Do not use a different chunk size.

Done when:
- Milestone 2 tasks are checked off.
- Build passes.
- Tests pass.
```

---

## Prompt 3 — Milestone 3: MUL reader

```
Implement Milestone 3 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md @TEST_PLAN.md first.

Task:
- Implement MulMapReader in WorldPainterUO.FileFormats.
- Use 196-byte blocks (4-byte header + 192-byte tile data). This is the real UO format.
- Parse block layout, tile IDs, and Z values.
- Support map0 through map5.
- Add golden-file tests using fixtures in tests/fixtures/golden/.
- Add a large-map smoke test.
- Run build and tests.
- Update TASKS.md.

Constraints:
- 196-byte blocks only. 192-byte synthetic fixtures must be updated to match.
- No fake parser logic. Isolate uncertain format details behind interfaces.
- Document any format assumptions explicitly.
- Reader must not depend on UI or rendering projects.

Done when:
- MulMapReader is implemented.
- All synthetic fixtures use 196-byte block format.
- Tests pass.
- Format assumptions are documented.
```

---

## Prompt 4 — Milestone 4: Project format

```
Implement Milestone 4 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Define the .uomap JSON schema.
- Implement project serializer and deserializer in WorldPainterUO.Project.
- Add autosave snapshot support.
- Add save/load tests.
- Run build and tests.
- Update TASKS.md.

Done when:
- Import a map, save as .uomap, reopen with no data loss.
- Tests pass.
```

---

## Prompt 5 — Milestone 5: MUL writer

```
Implement Milestone 5 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md @TEST_PLAN.md first.

Task:
- Implement MulMapWriter in WorldPainterUO.FileFormats.
- Export valid mapX.mul using 196-byte block layout.
- Add round-trip tests (import → export → import equivalence).
- Add export validation with diagnostics.
- Run build and tests.
- Update TASKS.md.

Done when:
- Round-trip tests pass.
- Export produces logically equivalent output to source.
```

---

## Prompt 6 — Milestone 6: UOP support

```
Implement Milestone 6 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md @TEST_PLAN.md first.

Task:
- Add a UOP container abstraction in WorldPainterUO.FileFormats.
- Implement UopMapReader for map0LegacyMUL.uop.
- Keep the existing UopMapWriter for UOP output (do not replace it).
- UltimaMapWriter handles MUL output; UopMapWriter handles UOP output.
- Both implement IMapFileWriter routed by format type.
- Add round-trip and compatibility tests.
- Run build and tests.
- Update TASKS.md.

Constraints:
- Treat UOP as a packaging layer over the same 196-byte land-tile data.
- Do not port statics from UOFiddler. Land tiles only.
- Document any uncertain UOP format details explicitly.

Done when:
- UOP import and export work.
- Round-trip is logically equivalent.
- Tests pass.
```

---

## Prompt 7 — Milestone 7: Avalonia shell

```
Implement Milestone 7 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Build the main Avalonia window.
- Add a viewport host with zoom and pan.
- Add a status bar showing coordinates, tile ID, and Z.
- Add a layer switcher.
- Add a minimap stub.
- Run build and tests.
- Update TASKS.md.

Constraints:
- Keep UI thin. Logic belongs in view models and Editor/Rendering projects.
- Use MVVM.
- Do not implement full rendering yet.

Done when:
- App launches.
- User can open and navigate a map.
```

---

## Prompt 8 — Milestone 8: Editing tools

```
Implement Milestone 8 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Implement the ICommand interface and CommandHistory (undo/redo stack) in WorldPainterUO.Editor.
- Each ICommand stores List<(ChunkCoord chunkIndex, MapTile[] before, MapTile[] after)> — only changed chunks.
- Implement the following tools in WorldPainterUO.Editor:
    - PaintBrushTool — paints terrain tile IDs with size, opacity, hardness, and randomization options.
    - FillTool — flood fill terrain by tile ID within a selection or map bounds.
    - TerrainReplaceTool — replace all instances of one tile ID with another across selection or full map.
    - HeightRaiseTool — increase Z for tiles under brush.
    - HeightLowerTool — decrease Z for tiles under brush.
    - SmoothTool — average Z values of neighboring tiles within brush radius.
    - FlattenTool — force all tiles under brush to a target Z value.
    - NoiseTool — apply random Z variation within a configurable range.
- Wire each tool to the CommandHistory so all edits are undoable and redoable.
- Add unit tests covering undo/redo round-trips for each tool.
- Run build and tests.
- Update TASKS.md.

Constraints:
- No UI code in the Editor project. Tools receive tile coordinates and parameters only.
- Undo/redo must restore exact tile ID and Z values.
- Chunk dirty flags must be set correctly on edit.

Done when:
- All 8 tools are implemented.
- Undo/redo round-trip tests pass for each tool.
- Build passes.
- TASKS.md updated.
```

---

## Prompt 9 — Milestone 9: Rendering

```
Implement Milestone 9 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Implement the map renderer in WorldPainterUO.Rendering using SkiaSharp.
- Support three view modes:
    - Radar View — color-coded terrain by tile ID using radarcol.mul data.
    - Terrain View — actual UO land tile artwork from art.mul / artLegacyMUL.uop.
    - Hybrid View — terrain artwork with radar color overlay.
- Implement chunk-based render cache: only redraw dirty chunks, cache clean ones.
- Implement zoom (min 0.1x, max 8x) and pan.
- Implement a minimap showing the full map at reduced resolution.
- Implement region overlay (draw named region boundaries).
- Implement grid overlay (toggle chunk or tile grid).
- Wire dirty chunk flags from the Editor layer to trigger targeted redraws.
- Add rendering tests for cache invalidation and view mode switching.
- Run build and tests.
- Update TASKS.md.

Constraints:
- Rendering must not depend on Editor or FileFormats directly — read from the Project model only.
- GPU acceleration via SkiaSharp where available.
- Terrain View must gracefully fall back to Radar View if art files are not present.

Done when:
- All three view modes render correctly.
- Zoom and pan work.
- Dirty chunk redraw is verified by test.
- Minimap renders.
- Build passes.
- TASKS.md updated.
```

---

## Prompt 10 — Milestone 10: Terrain rules engine

```
Implement Milestone 10 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Implement the TerrainPaletteSystem in WorldPainterUO.Editor.
    - Load palette definitions from tiledata.mul and radarcol.mul.
    - Auto-group tiles into named biomes: Ocean, Grass, Forest, Swamp, Snow, Desert, Mountain, Volcanic, Marsh, Road, Rock.
    - Each biome maps to a list of raw ushort tile IDs.
    - Serialize palette definitions to JSON for user customization.
- Implement the TerrainRulesEngine in WorldPainterUO.Editor.
    - Translate biome paint intent into valid UO tile IDs at export or brush-apply time.
    - Support weighted tile selection for natural variation (e.g. Grass → TileID 3 at 40%, 4 at 30%, 5 at 20%, 6 at 10%).
    - Support neighbor-aware transitions: detect adjacent biome boundaries and select blend tiles where defined.
- Add unit tests for palette loading, weighted selection, and neighbor transition logic.
- Run build and tests.
- Update TASKS.md.

Constraints:
- V1 TerrainLayer still stores raw tile IDs. The rules engine is an optional paint-assist layer.
- Do not change the MapTile or TerrainLayer data model.
- Biome definitions must be overridable via JSON config without recompilation.

Done when:
- Palette loads from tiledata.mul/radarcol.mul.
- Weighted selection produces correct distribution over N trials.
- Neighbor-aware transitions select blend tiles correctly.
- Tests pass.
- TASKS.md updated.
```

---

## Prompt 11 — Milestone 11: Export validation, autosave, preferences, logging

```
Implement Milestone 11 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Implement export validation in WorldPainterUO.FileFormats:
    - Check for invalid tile ID ranges (out of tiledata bounds).
    - Check for out-of-bounds Z values (must be -128 to +127).
    - Check for corrupt or incomplete chunk data.
    - Verify UOP packaging integrity before writing.
    - Return a structured ValidationResult with error list, warning list, and pass/fail.
- Implement autosave in WorldPainterUO.Project:
    - Configurable interval (default 5 minutes).
    - Save to a .uomap.autosave shadow file alongside the project.
    - Offer recovery on next open if autosave is newer than last manual save.
- Implement application preferences in WorldPainterUO.App:
    - Store in a JSON file in the OS user config directory.
    - Preferences: autosave interval, default map size, default UO data path, theme (light/dark).
- Implement structured logging throughout all projects using Microsoft.Extensions.Logging:
    - Log file import, export, validation events, errors, and undo/redo operations.
    - Log to file (rolling, max 10MB) and debug output.
- Add tests for validation result coverage and autosave recovery flow.
- Run build and tests.
- Update TASKS.md.

Done when:
- Export validation catches all listed error categories.
- Autosave creates and recovers shadow files.
- Preferences persist across sessions.
- Logging emits structured output to file.
- Tests pass.
- TASKS.md updated.
```

---

## Prompt 12 — Milestone 12: Procedural generation and packaging

```
Implement Milestone 12 only.

Read @AGENTS.md @ARCHITECTURE.md @TASKS.md first.

Task:
- Implement procedural map generators in WorldPainterUO.Editor:
    - GenerateIsland — single island with configurable seed, size, and biome style.
    - GenerateArchipelago — multiple islands, configurable count and spacing.
    - GenerateContinent — large landmass with interior biome variation.
    - GenerateWorld — full map generation combining continent + archipelago + ocean fill.
    - All generators accept: seed (int), map size (MapDimensions), biome style (enum).
    - Output: fully populated TerrainLayer and HeightLayer ready for editing.
- Add selection tools to WorldPainterUO.Editor:
    - RectangleSelection
    - LassoSelection (freehand polygon)
    - Support apply-to-selection mode for all existing editing tools.
- Add stamp tool:
    - Load terrain feature templates (mountain range, lake, river, forest cluster, road, island).
    - Place at cursor position with optional rotation.
    - Templates stored as JSON + tile data files under assets/stamps/.
- Publish a self-contained release build:
    - win-x64, linux-x64, osx-x64.
    - Single-file executable where platform supports it.
    - Include README, CHANGELOG, and license.
    - Add a GitHub Actions release workflow triggered by version tags.
- Run build and tests.
- Update TASKS.md.

Done when:
- All four generators produce valid terrain that passes export validation.
- Selection tools work with all editing tools.
- Stamp tool places at least 3 template types.
- Release builds produce runnable executables on all three platforms.
- All tests pass.
- TASKS.md updated.
```

---

## Between-milestone review prompt

```
Before implementing the next milestone, review the current repository and tell me:
1. What is complete.
2. What is risky or has known gaps.
3. What architectural decisions need confirmation.
4. The exact next smallest safe implementation step.
Do not write any code until I confirm.
```