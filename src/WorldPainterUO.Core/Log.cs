using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WorldPainterUO.Core;

/// <summary>
/// Static log accessor. Configured once during application startup.
/// All projects use this to obtain typed loggers.
/// </summary>
public static class Log
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    /// <summary>Sets the logger factory (call once at startup).</summary>
    public static void SetFactory(ILoggerFactory factory) =>
        _factory = factory ?? NullLoggerFactory.Instance;

    /// <summary>Gets a logger for the given type.</summary>
    public static ILogger<T> For<T>() =>
        _factory.CreateLogger<T>();

    /// <summary>Gets a named logger.</summary>
    public static ILogger For(string categoryName) =>
        _factory.CreateLogger(categoryName);
}
