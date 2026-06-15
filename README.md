# WorldPainterUO

WorldPainterUO is a modern Ultima Online world-building application for painting, editing, previewing, and exporting UO land maps with a modern desktop workflow.

The project targets **C# / .NET 8**, **Avalonia UI**, **SkiaSharp**, JSON project serialization, and a custom `.uomap` project format. The goal is to preserve all terrain and elevation data required to round-trip between editable project files and valid UO MUL/UOP map outputs.

## Goals

- Import existing UO land maps.
- Edit terrain and elevation using paint-style tools.
- Save non-destructive `.uomap` projects.
- Preview terrain in radar, terrain, and hybrid modes.
- Export valid `mapX.mul` and `map0LegacyMUL.uop` files.
- Produce results usable in ServUO shard workflows.

## Repository purpose

This repository is structured so Codex can implement the app milestone-by-milestone from specification files instead of improvising architecture.

## Core documents

| File | Purpose |
|---|---|
| `AGENTS.md` | Permanent implementation rules for Codex |
| `SPEC.md` | Product requirements and scope |
| `ARCHITECTURE.md` | Target system design |
| `IMPLEMENTATION_PLAN.md` | Phased roadmap |
| `TASKS.md` | Milestone checklist |
| `TEST_PLAN.md` | Required test strategy |
| `PROMPTS.md` | Copy/paste prompts to drive Codex |

## Suggested workflow with Codex

1. Start Codex with Prompt 0 from `PROMPTS.md` (plan only, no code).
2. Confirm plan, then run Prompt 1 for Milestone 1.
3. Review the diff and verify the build passes.
4. Continue one milestone at a time.

## Solution layout (target)

```text
WorldPainterUO/
├── .github/
│   └── workflows/
│       └── dotnet-ci.yml
├── src/
│   ├── WorldPainterUO.Core/
│   ├── WorldPainterUO.FileFormats/
│   ├── WorldPainterUO.Project/
│   ├── WorldPainterUO.Rendering/
│   ├── WorldPainterUO.Editor/
│   └── WorldPainterUO.App/
├── tests/
│   ├── WorldPainterUO.Tests/
│   └── fixtures/
│       ├── golden/
│       └── synthetic/
├── AGENTS.md
├── ARCHITECTURE.md
├── IMPLEMENTATION_PLAN.md
├── PROMPTS.md
├── README.md
├── SPEC.md
├── TASKS.md
└── TEST_PLAN.md
```
