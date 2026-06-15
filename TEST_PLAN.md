# TEST_PLAN.md

## Testing goals

Prove that file fidelity and editing correctness are maintained across import, edit, save, load, and export workflows.

## Test categories

### Unit tests

Cover:

- Core map tile and chunk invariants.
- Dirty-chunk tracking.
- Parser helpers (binary reading utilities).
- Encoder helpers.
- Command execution and reversal.
- Terrain rules behavior.

### Round-trip tests

Cover:

- `mapX.mul` import → export → re-import logical equivalence.
- `map0LegacyMUL.uop` import → export → re-import logical equivalence.
- `.uomap` save → load equivalence.

### Integration tests

Cover:

- Import → edit → save project → reopen → export workflow.
- Undo/redo after mixed terrain and height edits.
- Large-map load smoke tests.

### Performance validation

Track:

- Large-map load time.
- Chunk redraw counts during brush edits.
- Memory usage during viewport interaction.

## Required test assets

- Small synthetic map fixtures (generated in test setup).
- Known-good golden input files (place in `tests/fixtures/golden/`).
- Expected output fixtures where practical.

## Fixture placement

```text
tests/
  fixtures/
    golden/          <- place real UO map samples here
    synthetic/       <- generated minimal maps for unit tests
```

## Done criteria

No milestone is complete unless:

- New behavior is covered by tests where feasible.
- Build passes.
- Tests pass.
- Known gaps are explicitly documented.
