namespace WorldPainterUO.FileFormats.Uop;

public sealed class UopFormatException : Exception
{
    public UopFormatException(string message) : base(message) { }

    public UopFormatException(string message, Exception inner) : base(message, inner) { }
}
