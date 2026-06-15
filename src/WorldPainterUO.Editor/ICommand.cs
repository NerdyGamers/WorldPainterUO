using WorldPainterUO.Core;

namespace WorldPainterUO.Editor;

/// <summary>
/// A reversible edit operation on a <see cref="WorldMap"/>.
/// Stores chunk-level before/after snapshots for undo/redo.
/// </summary>
public interface ICommand
{
    string Description { get; }

    /// <summary>Applies the "after" state. Returns false if no tiles changed.</summary>
    bool Execute(WorldMap map);

    /// <summary>Restores the "before" state. Returns false if nothing to restore.</summary>
    bool Undo(WorldMap map);
}
