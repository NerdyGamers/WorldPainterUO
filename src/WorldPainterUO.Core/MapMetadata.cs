namespace WorldPainterUO.Core;

public readonly record struct MapMetadata(
    string Facet,
    SourceFileType SourceFileType,
    int Version = 1
);
