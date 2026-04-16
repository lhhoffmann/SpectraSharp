using SpectraEngine.ModTranspiler.Pipeline;

namespace SpectraEngine.ModTranspiler;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: ModTranspiler <path/to/mod.jar> [output-dir]");
            Console.Error.WriteLine("  output-dir defaults to Bridge/Mods/");
            return 1;
        }

        string jarPath   = args[0];
        string outputDir = args.Length > 1 ? args[1] : "Bridge/Mods";

        if (!File.Exists(jarPath))
        {
            Console.Error.WriteLine($"[Error] JAR not found: {jarPath}");
            return 1;
        }

        if (!File.Exists("tools/decompiler/vineflower.jar"))
        {
            Console.Error.WriteLine("[Error] tools/decompiler/vineflower.jar not found.");
            Console.Error.WriteLine("        Run the bootstrap script or download Vineflower manually.");
            return 1;
        }

        string modName = Path.GetFileNameWithoutExtension(jarPath);
        Console.WriteLine($"[ModTranspiler] Processing: {modName}");

        try
        {
            var result = TranspilerPipeline.Run(jarPath, modName, outputDir);

            if (result.Success)
            {
                Console.WriteLine($"[ModTranspiler] Done — output: {result.DllPath}");
                Console.WriteLine($"[ModTranspiler] {result.BlocksGenerated} blocks, " +
                                  $"{result.ItemsGenerated} items, " +
                                  $"{result.HooksGenerated} hooks, " +
                                  $"{result.TodoCount} TODOs require manual review.");
                return 0;
            }
            else
            {
                Console.Error.WriteLine("[ModTranspiler] Compilation failed:");
                foreach (string err in result.Errors)
                    Console.Error.WriteLine($"  {err}");
                return 2;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ModTranspiler] Unexpected error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 99;
        }
    }
}
