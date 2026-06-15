using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace WorldPainterUO.App.Logging;

/// <summary>
/// Simple file logger with rolling (max 10MB per file) and debug output.
/// </summary>
public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _basePath;
    private readonly int _maxBytes = 10 * 1024 * 1024;

    private static readonly object Lock = new();

    public FileLogger(string categoryName, string basePath)
    {
        _categoryName = categoryName;
        _basePath = basePath;
        var dir = Path.GetDirectoryName(basePath);
        if (dir?.Length > 0)
            Directory.CreateDirectory(dir);
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Trace;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
        Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var level = logLevel.ToString()[..4];
        var line = $"[{timestamp}] [{level}] [{_categoryName}] {message}";

        if (exception is not null)
            line += $"\n  Exception: {exception}";

        // Debug output
        Debug.WriteLine(line);

        // File output with rolling
        lock (Lock)
        {
            try
            {
                var currentPath = _basePath;
                if (File.Exists(currentPath) && new FileInfo(currentPath).Length >= _maxBytes)
                {
                    var rotatedPath = _basePath + ".1";
                    if (File.Exists(rotatedPath))
                        File.Delete(rotatedPath);
                    File.Move(currentPath, rotatedPath);
                }

                File.AppendAllText(currentPath, line + Environment.NewLine);
            }
            catch
            {
                // Best-effort file logging
            }
        }
    }
}

/// <summary>Provider for <see cref="FileLogger"/>.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _basePath;

    public FileLoggerProvider(string basePath)
    {
        _basePath = basePath;
    }

    public ILogger CreateLogger(string categoryName) =>
        new FileLogger(categoryName, _basePath);

    public void Dispose() { }
}
