# WorldPainterUO Specification

## Product summary

WorldPainterUO is a modern Ultima Online world-building application that allows users to paint and edit UO maps using familiar image-editing workflows while preserving all terrain and elevation data required to generate valid MUL and UOP map files.

The application is intended to replace legacy map-editing workflows with a modern, paint-oriented desktop experience.

## Primary users

- UO shard developers
- UO world builders
- Administrators maintaining custom maps
- Technical users migrating from CentrED-style tools

## V1 scope

### File support
- Import `map0.mul` through `map5.mul`
- Import `map0LegacyMUL.uop`
- Export `map0.mul` through `map5.mul`
- Export `map0LegacyMUL.uop`

### Editing
- Save and load `.uomap` projects
- Terrain paint brush
- Height paint brush
- Fill tool
- Terrain replace tool
- Smooth height tool
- Flatten height tool
- Undo/redo (unlimited via command pattern)

### Viewing
- Radar view
- Terrain view
- Hybrid view
- Grid overlay
- Region overlay
- Minimap

### Export
- Pre-export validation with diagnostics
- ServUO-compatible land-map output

## Out of scope for V1

- Statics editing
- House placement
- Dungeon builder
- Multi-user collaboration
- Server synchronization
- Plugin marketplace
- AI generation
- Static placement editor

## Core concept — bidirectional workflow

```
map0.mul / map0LegacyMUL.uop
            ↓
     WorldPainterUO
            ↓
    Editable .uomap
            ↓
    External Editing
            ↓
     WorldPainterUO
            ↓
map0.mul / map0LegacyMUL.uop
```

## Functional requirements

### Data fidelity
The application must preserve tile IDs and Z values exactly on import unless changed by the user.

### Project format
The application must save a non-destructive `.uomap` project format that stores editable state separately from export state.

### Rendering
The application must render large maps efficiently with zoom, pan, overlays, and partial chunk-based redraw.

### Validation
The application must validate maps before export and report issues clearly.

## Non-functional requirements

- Target large map sizes including Britannia-scale (6144x4096).
- Use chunking, caching, and partial redraw to remain responsive.
- Support autosave recovery behavior.
- Keep architecture modular for future plugins and V2/V3 features.

## Success criteria

A user with no UO mapping experience should be able to:

1. Create or import a map.
2. Paint terrain visually.
3. Paint elevations.
4. Preview the world.
5. Export a valid MUL/UOP map.
6. Load the map in a ServUO workflow without manual repair.
