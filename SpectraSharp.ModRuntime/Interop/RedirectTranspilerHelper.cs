using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SpectraSharp.ModRuntime.Interop;

/// <summary>
/// Static helper called by the DynamicMethod transpilers emitted in
/// <see cref="HarmonyBridge.BuildRedirectTranspiler"/>.
///
/// Walks the <see cref="CodeInstruction"/> stream of the patched method and
/// replaces every call to a method whose simple name matches
/// <paramref name="targetCallName"/> with a call to <paramref name="replacement"/>.
///
/// This implements the Mixin @Redirect contract: all call sites of a specific
/// method inside the target body are replaced by the mixin's redirect handler.
/// </summary>
public static class RedirectTranspilerHelper
{
    public static IEnumerable<CodeInstruction> Replace(
        IEnumerable<CodeInstruction> instructions,
        string      targetCallName,
        MethodBase  replacement)
    {
        bool patched = false;

        foreach (var inst in instructions)
        {
            if ((inst.opcode == OpCodes.Call || inst.opcode == OpCodes.Callvirt) &&
                inst.operand is MethodBase target &&
                target.Name.Equals(targetCallName, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    $"[HarmonyBridge] @Redirect: {target.DeclaringType?.Name}.{target.Name}" +
                    $" → {replacement.DeclaringType?.Name}.{replacement.Name}");
                yield return new CodeInstruction(inst.opcode, replacement);
                patched = true;
            }
            else
            {
                yield return inst;
            }
        }

        if (!patched && !string.IsNullOrEmpty(targetCallName))
        {
            Console.Error.WriteLine(
                $"[HarmonyBridge] @Redirect: call site '{targetCallName}' not found — patch is a no-op.");
        }
    }
}
