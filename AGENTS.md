# AGENTS.md

You are building **WorldPainterUO**, a modern Ultima Online world-building application.

## Product goal

Create a modern UO map editor that can import, edit, preview, and export valid UO land maps while preserving terrain and elevation data needed for round-trip MUL/UOP workflows.

## Tech stack

- C# / .NET 8
- Avalonia UI
- SkiaSharp
- JSON project serialization
- Custom project format: `.uomap`

## Non-negotiable rules

- Never use placeholder code for core file format logic.
- Never fake UO format support; if a format detail is uncertain, isolate it behind an interface and cover it with tests and a documented TODO.
- Preserve exact imported land tile IDs and Z values unless the user edits them.
- Use a chunked internal map model.
- Keep file IO, renderer, editor tools, and project serialization in separate assemblies/namespaces.
- Prefer small, reviewable commits per milestone.
- Add tests for every parser, encoder, command, and round-trip behavior.
- Do not start procedural generation until core import/export, project save/load, viewport, and basic tools are complete.
- Do not add statics, houses, multis, networking, plugins, or AI generation in V1 unless explicitly requested.

## Required project structure

```
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

| Module | Owns |
|---|---|
| `Core` | Domain model: tile, chunk, layer, metadata, bounds |
| `FileFormats` | MUL/UOP readers and writers |
| `Project` | `.uomap` save/load, autosave, versioning |
| `Rendering` | Viewport rendering, radar, overlays |
| `Editor` | Commands, tools, undo/redo, terrain rules |
| `App` | Avalonia UI shell, MVVM view models |
| `Tests` | All test projects |

## Coding standards

- Use nullable reference types.
- Use async IO where appropriate.
- Use dependency injection where useful, but keep architecture simple.
- Public APIs require XML docs.
- Favor immutable records for metadata and config models.
- Keep UI thin; business logic belongs outside views.
- Use MVVM for Avalonia.
- Every milestone must leave the solution in a buildable state.

## Testing requirements

- Unit tests for map block parsing and encoding.
- Round-trip tests for import/export.
- Project save/load tests for `.uomap`.
- Command undo/redo tests.
- Integration tests for milestone-complete workflows.

## Workflow for each Codex pass

1. Read `SPEC.md`, `ARCHITECTURE.md`, `IMPLEMENTATION_PLAN.md`, `TASKS.md`, and `TEST_PLAN.md`.
2. Propose or confirm the current milestone.
3. Implement only that milestone.
4. Run build and tests.
5. Update docs and `TASKS.md`.
6. Summarize what changed, what remains, and any open questions.

## Done criteria for each milestone

A milestone is not complete until:

- Relevant code is implemented.
- Solution builds cleanly.
- Tests pass.
- Documentation is updated.
- `TASKS.md` reflects completed work.
- Known risks or unknown format details are documented.
