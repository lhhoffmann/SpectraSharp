namespace SpectraSharp.IO;

/// <summary>
/// Thrown when SpectraSharp cannot locate the legacy game JAR that supplies
/// all runtime assets.  The user must own and supply this file; SpectraSharp
/// never distributes or modifies game files.
/// </summary>
public sealed class VanillaNotFoundException : FileNotFoundException
{
    public VanillaNotFoundException(string jarPath)
        : base(BuildMessage(jarPath), jarPath) { }

    private static string BuildMessage(string jarPath) => $"""
        SpectraSharp could not find the legacy game JAR (1.0).

        Expected location:
          {jarPath}

        To fix this:
          1. Install the original game through its official launcher.
          2. Launch version 1.0 at least once so the launcher downloads the JAR.
          3. Start SpectraSharp again.

        SpectraSharp never distributes or modifies game files.
        """;
}
