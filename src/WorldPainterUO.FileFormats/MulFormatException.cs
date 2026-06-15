namespace WorldPainterUO.FileFormats;

public sealed class MulFormatException : Exception
{
    public MulFormatException(string message) : base(message) { }

    public MulFormatException(string message, Exception inner) : base(message, inner) { }
}
