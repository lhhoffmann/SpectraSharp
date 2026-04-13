namespace SpectraSharp.ModTranspiler.Mappings;

/// <summary>
/// Maps Java primitive and common types to C# equivalents.
/// </summary>
static class TypeMap
{
    static readonly Dictionary<string, string> Map = new()
    {
        ["void"]    = "void",
        ["boolean"] = "bool",
        ["byte"]    = "byte",
        ["short"]   = "short",
        ["int"]     = "int",
        ["long"]    = "long",
        ["float"]   = "float",
        ["double"]  = "double",
        ["char"]    = "char",
        ["String"]  = "string",
        ["Object"]  = "object",

        // Common Java collections → C#
        ["List"]       = "List",
        ["ArrayList"]  = "List",
        ["HashMap"]    = "Dictionary",
        ["Map"]        = "Dictionary",
        ["Set"]        = "HashSet",
        ["Iterator"]   = "IEnumerator",

        // Java standard classes
        ["Random"]    = "Random",
        ["Math"]      = "Math",
        ["System"]    = "Console",  // Java System.out.println → Console
    };

    public static string ToCSharp(string javaType)
    {
        // Strip generic parameters for lookup, preserve for output
        int genericStart = javaType.IndexOf('<');
        string baseType  = genericStart > 0 ? javaType[..genericStart] : javaType;
        string generics  = genericStart > 0 ? javaType[genericStart..] : "";

        if (Map.TryGetValue(baseType, out string? mapped))
            return mapped + generics;

        return javaType; // Unknown type — keep as-is, let TODO comments surface it
    }
}
