# WorldPainterUO

WorldPainterUO is a modern Ultima Online world-building application for painting, editing, previewing, and exporting UO land maps with a modern desktop workflow.

The project targets **C# / .NET 8**, **Avalonia UI**, **SkiaSharp**, JSON project serialization, and a custom `.uomap` project format. The goal is to preserve all terrain and elevation data required to round-trip between editable project files and valid UO MUL/UOP map outputs.

## Goals

- Import existing UO land maps (`.mul`, `.uop`).
- Edit terrain and elevation using paint-style tools.
- Save and load `.uomap` project files with full round-trip fidelity.
- Preview terrain in radar, terrain, and hybrid modes.
- Export valid `mapX.mul` and `map0LegacyMUL.uop` files.
- Produce results usable in ServUO shard workflows.

## Features

- Import/export MUL and UOP map files with round-trip validation
- Paint terrain tiles (brush, fill, replace) with configurable radius/strength
- Raise/lower/smooth/flatten/noise height editing
- Undo/redo with unlimited command history
- Procedural map generation (island, archipelago, continent, world)
- Selection tools (rectangle, lasso) for targeted editing
- Stamp tool for placing terrain templates
- Three view modes: radar, terrain, hybrid
- Tile grid and chunk grid overlays
- Minimap with cached rendering
- File menu: New, Open (`.mul`/`.uop`/`.uomap`), Save, Export
- Recent files list with persistence
- Brush preview circle, status bar with tile info

### Keyboard shortcuts

| Key | Action |
|-----|--------|
| `V` | Pan |
| `B` | Paint brush |
| `F` | Fill |
| `R` | Raise height |
| `L` | Lower height |
| `S` | Smooth height |
| `G` | Flatten height |
| `N` | Noise |
| `E` | Replace terrain |
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Ctrl+O` | Open |
| `Ctrl+E` | Export |
| `Ctrl+=` / `Ctrl+-` | Zoom in / out |
| `Ctrl+0` | Reset zoom |

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

## Building

```bash
dotnet build
```

## Running tests

```bash
dotnet test
```

## Publishing

Self-contained single-file executables for all three platforms:

```powershell
# All platforms
.\scripts\publish.ps1 v1.0.0

# Or individually:
dotnet publish src/WorldPainterUO.App -c Release -r win-x64  --self-contained true /p:PublishSingleFile=true
dotnet publish src/WorldPainterUO.App -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
dotnet publish src/WorldPainterUO.App -c Release -r osx-x64  --self-contained true
```

Outputs go to `publish/v1.0.0/`.

## GitHub Releases

Tag a commit with `v*` (e.g., `v1.0.0`) to trigger the release workflow, which builds all three platforms and uploads archives as release assets.

## Solution layout

```text
WorldPainterUO/
├── .github/
│   └── workflows/
│       ├── dotnet-ci.yml
│       └── release.yml
├── assets/
│   └── stamps/
│       ├── mountain_range.json
│       ├── lake.json
│       └── forest_cluster.json
├── scripts/
│   └── publish.ps1
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
├── CHANGELOG.md
├── IMPLEMENTATION_PLAN.md
├── PROMPTS.md
├── README.md
├── SPEC.md
├── TASKS.md
└── TEST_PLAN.md
```
