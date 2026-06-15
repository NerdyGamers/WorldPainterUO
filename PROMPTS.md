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
- Define MapChunk (tile array, dirty flag, bounds).
- Define MapDimensions and MapBounds.
- Define MapMetadata.
- Define TerrainLayer and HeightLayer abstractions.
- Implement dirty-chunk tracking.
- Add unit tests for all new types.
- Run build and tests.
- Update TASKS.md.

Constraints:
- Core project has no dependency on FileFormats, Rendering, or App.
- Models are immutable or clearly mutable with intent.
- No UI code.

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
- Parse block layout, tile IDs, and Z values.
- Support map0 through map5.
- Add golden-file tests using fixtures in tests/fixtures/golden/.
- Add a large-map smoke test.
- Run build and tests.
- Update TASKS.md.

Constraints:
- No fake parser logic. Isolate uncertain format details behind interfaces.
- Document any format assumptions explicitly.
- Reader must not depend on UI or rendering projects.

Done when:
- MulMapReader is implemented.
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
- Export valid mapX.mul block layout.
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
- Implement UopMapWriter for map0LegacyMUL.uop.
- Add round-trip and compatibility tests.
- Run build and tests.
- Update TASKS.md.

Constraints:
- Treat UOP as a packaging layer over the same land-tile data.
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

## Between-milestone review prompt

```
Before implementing the next milestone, review the current repository and tell me:
1. What is complete.
2. What is risky or has known gaps.
3. What architectural decisions need confirmation.
4. The exact next smallest safe implementation step.
Do not write any code until I confirm.
```
