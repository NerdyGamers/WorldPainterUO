using Microsoft.Extensions.Logging;
using WorldPainterUO.Core;

namespace WorldPainterUO.Editor;

public sealed class CommandHistory
{
    private readonly Stack<ICommand> _undoStack = new();
    private readonly Stack<ICommand> _redoStack = new();
    private int _capacity = 256;

    /// <summary>Maximum commands to keep in the undo stack.</summary>
    public int Capacity
    {
        get => _capacity;
        set
        {
            _capacity = Math.Max(1, value);
            while (_undoStack.Count > _capacity)
                _undoStack.Pop();
        }
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public string? UndoDescription => _undoStack.TryPeek(out var c) ? c.Description : null;
    public string? RedoDescription => _redoStack.TryPeek(out var c) ? c.Description : null;

    public event Action? StateChanged;

    private readonly ILogger _logger = Log.For("CommandHistory");

    /// <summary>Executes a command and pushes it onto the undo stack.</summary>
    public void Execute(ICommand command, WorldMap map)
    {
        if (command.Execute(map))
        {
            _undoStack.Push(command);
            _redoStack.Clear();
            TrimExcess();
            StateChanged?.Invoke();
            _logger.LogInformation("Command executed: {Desc} (undo stack: {Count})",
                command.Description, _undoStack.Count);
        }
    }

    /// <summary>Undoes the most recent command.</summary>
    public bool Undo(WorldMap map)
    {
        if (_undoStack.Count == 0)
            return false;

        var command = _undoStack.Pop();

        if (command.Undo(map))
        {
            _redoStack.Push(command);
            StateChanged?.Invoke();
            _logger.LogInformation("Command undone: {Desc}", command.Description);
            return true;
        }

        // Undo failed (nothing to restore); don't push to redo
        StateChanged?.Invoke();
        _logger.LogWarning("Command undo failed: {Desc}", command.Description);
        return false;
    }

    /// <summary>Redoes the most recently undone command.</summary>
    public bool Redo(WorldMap map)
    {
        if (_redoStack.Count == 0)
            return false;

        var command = _redoStack.Pop();

        if (command.Execute(map))
        {
            _undoStack.Push(command);
            StateChanged?.Invoke();
            _logger.LogInformation("Command redone: {Desc}", command.Description);
            return true;
        }

        StateChanged?.Invoke();
        _logger.LogWarning("Command redo failed: {Desc}", command.Description);
        return false;
    }

    /// <summary>Clears both stacks.</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke();
        _logger.LogInformation("Command history cleared");
    }

    private void TrimExcess()
    {
        while (_undoStack.Count > _capacity)
            _undoStack.Pop();
    }
}
