// xUnit runs test classes from different collections in parallel by default.
// All tests using the Ultima SDK (whose Files.MulPath is a global static
// dictionary) must share this collection to avoid state-leak races.

namespace WorldPainterUO.Tests;

[CollectionDefinition("UltimaSDK", DisableParallelization = true)]
public sealed class UltimaSdkCollection;
