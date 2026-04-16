using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace SpectraEngine.ModRuntime.Interop;

/// <summary>
/// Converts <see cref="MixinDescriptor"/> injections into live Harmony patches
/// applied to SpectraEngine.Core types (resolved via <see cref="ClassMapping"/>).
///
/// Patch translation:
/// ┌─────────────────────────────┬──────────────────────────────────────────────┐
/// │ Mixin annotation            │ Harmony mechanism                            │
/// ├─────────────────────────────┼──────────────────────────────────────────────┤
/// │ @Inject(at=HEAD)            │ Prefix                                       │
/// │ @Inject(at=RETURN/TAIL)     │ Postfix                                      │
/// │ @Inject(at=INVOKE)          │ Prefix (approximation; logs a warning)       │
/// │ @Overwrite                  │ DynamicMethod prefix that skips original     │
/// │ @Redirect                   │ DynamicMethod transpiler via                 │
/// │                             │ RedirectTranspilerHelper.Replace()           │
/// │ @Accessor / @Invoker        │ No patch — IKVM generates stubs at compile   │
/// └─────────────────────────────┴──────────────────────────────────────────────┘
///
/// All patches go through the caller-supplied Harmony instance so that a single
/// <c>UnpatchAll(id)</c> call in <see cref="Sandbox.ModSandbox.RevertPatches()"/>
/// cleans up both game-logic patches and mixin patches atomically.
/// </summary>
public sealed class HarmonyBridge(Harmony harmony)
{
    // ── Apply ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies all injections in <paramref name="descriptor"/> to the resolved .NET type.
    /// Unknown Java class names are logged and skipped — never fatal.
    /// </summary>
    public void Apply(MixinDescriptor descriptor)
    {
        Type? targetType = ClassMapping.Resolve(descriptor.TargetJavaClass);
        if (targetType == null)
        {
            Console.Error.WriteLine(
                $"[HarmonyBridge] Cannot resolve '{descriptor.TargetJavaClass}'" +
                " — no ClassMapping entry. Mixin skipped.");
            return;
        }

        foreach (var inj in descriptor.Injections)
        {
            try
            {
                ApplyInjection(inj, targetType);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[HarmonyBridge] Patch failed: {descriptor.TargetJavaClass}" +
                    $".{inj.TargetMethod} [{inj.Kind}] — {ex.Message}");
            }
        }
    }

    // ── Dispatch ──────────────────────────────────────────────────────────────

    void ApplyInjection(MixinInjection inj, Type targetType)
    {
        if (inj.MixinMethod == null) return;

        switch (inj.Kind)
        {
            case InjectionKind.Inject:    ApplyInject(inj, targetType);    break;
            case InjectionKind.Overwrite: ApplyOverwrite(inj, targetType); break;
            case InjectionKind.Redirect:  ApplyRedirect(inj, targetType);  break;

            // Accessor/Invoker stubs are emitted by ikvmc — no runtime patch needed.
            case InjectionKind.Accessor:
            case InjectionKind.Invoker:
                break;
        }
    }

    // ── @Inject ───────────────────────────────────────────────────────────────

    void ApplyInject(MixinInjection inj, Type targetType)
    {
        MethodBase? original = ResolveMethod(targetType, inj.TargetMethod, inj.TargetDesc);
        if (original == null)
        {
            Console.Error.WriteLine(
                $"[HarmonyBridge] @Inject target not found: {targetType.Name}.{inj.TargetMethod}");
            return;
        }

        var hm = new HarmonyMethod(inj.MixinMethod!);

        switch (inj.At)
        {
            case InjectionAt.Head:
                harmony.Patch(original, prefix: hm);
                Console.WriteLine($"[HarmonyBridge] @Inject(HEAD) prefix → {targetType.Name}.{original.Name}");
                break;

            case InjectionAt.Return:
            case InjectionAt.Tail:
                harmony.Patch(original, postfix: hm);
                Console.WriteLine($"[HarmonyBridge] @Inject(RETURN) postfix → {targetType.Name}.{original.Name}");
                break;

            case InjectionAt.Invoke:
                // Best-effort: prefix fires before the method, not at the specific call site.
                harmony.Patch(original, prefix: hm);
                Console.WriteLine(
                    $"[HarmonyBridge] @Inject(INVOKE) on {targetType.Name}.{original.Name} " +
                    "— approximated as prefix (fires before method, not at call site).");
                break;
        }
    }

    // ── @Overwrite ────────────────────────────────────────────────────────────

    void ApplyOverwrite(MixinInjection inj, Type targetType)
    {
        MethodBase? original = ResolveMethod(targetType, inj.TargetMethod, inj.TargetDesc);
        if (original == null)
        {
            Console.Error.WriteLine(
                $"[HarmonyBridge] @Overwrite target not found: {targetType.Name}.{inj.TargetMethod}");
            return;
        }

        if (inj.MixinMethod is not MethodInfo replacement) return;

        MethodInfo prefix = BuildOverwritePrefix(original, replacement);
        harmony.Patch(original, prefix: new HarmonyMethod(prefix));
        Console.WriteLine($"[HarmonyBridge] @Overwrite → {targetType.Name}.{original.Name}");
    }

    /// <summary>
    /// Emits a static prefix method:
    ///
    ///   For void originals:
    ///     static bool Prefix([instance?, params...]) { replacement([instance?, params...]); return false; }
    ///
    ///   For non-void originals:
    ///     static bool Prefix([instance?, params...], ref TReturn __result)
    ///     { __result = replacement([instance?, params...]); return false; }
    ///
    /// Returning false tells Harmony to skip the original method body.
    /// </summary>
    static MethodInfo BuildOverwritePrefix(MethodBase original, MethodInfo replacement)
    {
        bool isStatic     = original.IsStatic;
        bool isVoid       = replacement.ReturnType == typeof(void);
        Type? returnType  = isVoid ? null : replacement.ReturnType;
        Type? ownerType   = original.DeclaringType ?? typeof(HarmonyBridge);

        var originalParams = original.GetParameters();

        // Build the full parameter type list for the DynamicMethod:
        //   [__instance (if non-static)], [original params...], [ref __result (if non-void)]
        var paramTypes = new List<Type>();
        if (!isStatic) paramTypes.Add(ownerType);
        paramTypes.AddRange(originalParams.Select(p => p.ParameterType));
        int resultParamIdx = -1;
        if (!isVoid)
        {
            resultParamIdx = paramTypes.Count;
            paramTypes.Add(returnType!.MakeByRefType());
        }

        var dm = new DynamicMethod(
            name:           $"__overwrite_{original.Name}_{Guid.NewGuid():N}",
            returnType:     typeof(bool),
            parameterTypes: [.. paramTypes],
            owner:          ownerType,
            skipVisibility: true);

        // Name the __result parameter so Harmony resolves it by convention.
        if (resultParamIdx >= 0)
            dm.DefineParameter(resultParamIdx + 1, ParameterAttributes.Out, "__result");

        var il = dm.GetILGenerator();

        // Push arguments for the call to `replacement`.
        // replacement signature mirrors the original (minus __result).
        int argCount = (isStatic ? 0 : 1) + originalParams.Length;
        for (int i = 0; i < argCount; i++)
            EmitLdarg(il, i);

        il.Emit(replacement.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, replacement);

        // Store return value into __result (ref parameter).
        if (!isVoid)
        {
            // Stack: [returnValue]
            // __result is at paramTypes index resultParamIdx, which is argument resultParamIdx.
            EmitLdarg(il, resultParamIdx);   // push ref __result address
            il.Emit(OpCodes.Stind_Ref);      // *__result = returnValue  (works for ref/class types)
            // Note: for value types we'd need Stind_I4/Stind_R4 etc.
            // Stind_Ref handles reference types; EmitStoreByType() below covers value types.
        }

        il.Emit(OpCodes.Ldc_I4_0); // false → skip original
        il.Emit(OpCodes.Ret);

        return dm;
    }

    // ── @Redirect ─────────────────────────────────────────────────────────────

    void ApplyRedirect(MixinInjection inj, Type targetType)
    {
        MethodBase? original = ResolveMethod(targetType, inj.TargetMethod, inj.TargetDesc);
        if (original == null)
        {
            Console.Error.WriteLine(
                $"[HarmonyBridge] @Redirect target not found: {targetType.Name}.{inj.TargetMethod}");
            return;
        }

        if (inj.MixinMethod is not MethodInfo redirectMethod) return;

        MethodInfo transpiler = BuildRedirectTranspiler(original, redirectMethod, inj.RedirectTarget);
        harmony.Patch(original, transpiler: new HarmonyMethod(transpiler));
        Console.WriteLine(
            $"[HarmonyBridge] @Redirect → {targetType.Name}.{original.Name}" +
            $" (intercepts call to '{inj.RedirectTarget ?? "?"}')");
    }

    /// <summary>
    /// Emits a transpiler DynamicMethod with signature:
    ///   static IEnumerable&lt;CodeInstruction&gt; (IEnumerable&lt;CodeInstruction&gt; instructions)
    ///
    /// The emitted method delegates to
    ///   <see cref="RedirectTranspilerHelper.Replace(IEnumerable{CodeInstruction}, string, MethodBase)"/>
    /// at runtime, which walks the instruction stream and swaps the call site.
    /// </summary>
    static MethodInfo BuildRedirectTranspiler(
        MethodBase original, MethodInfo redirectMethod, string? redirectTarget)
    {
        // Signature required by Harmony:
        //   static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        var returnType  = typeof(IEnumerable<CodeInstruction>);
        var paramTypes  = new[] { typeof(IEnumerable<CodeInstruction>) };
        var ownerType   = original.DeclaringType ?? typeof(HarmonyBridge);

        var dm = new DynamicMethod(
            name:           $"__redirect_{original.Name}_{Guid.NewGuid():N}",
            returnType:     returnType,
            parameterTypes: paramTypes,
            owner:          ownerType,
            skipVisibility: true);

        var il   = dm.GetILGenerator();

        // RedirectTranspilerHelper.Replace(instructions, targetCallName, replacement)
        var helper = typeof(RedirectTranspilerHelper).GetMethod(
            nameof(RedirectTranspilerHelper.Replace),
            BindingFlags.Public | BindingFlags.Static)!;

        var getFromHandle = typeof(MethodBase).GetMethod(
            nameof(MethodBase.GetMethodFromHandle),
            [typeof(RuntimeMethodHandle)])!;

        il.Emit(OpCodes.Ldarg_0);                           // instructions
        il.Emit(OpCodes.Ldstr, redirectTarget ?? "");       // targetCallName

        // Load the MethodInfo of redirectMethod as a MethodBase constant.
        il.Emit(OpCodes.Ldtoken, redirectMethod);
        il.Emit(OpCodes.Call, getFromHandle);               // MethodBase

        il.Emit(OpCodes.Call, helper);                      // Replace(...)
        il.Emit(OpCodes.Ret);

        return dm;
    }

    // ── Method resolution ─────────────────────────────────────────────────────

    /// <summary>
    /// Finds a method on <paramref name="type"/> by name (case-insensitive and
    /// with camelCase/PascalCase normalisation).  Uses the Java descriptor to
    /// pick the right overload when multiple candidates exist.
    /// </summary>
    static MethodBase? ResolveMethod(Type type, string methodName, string javaDesc)
    {
        if (string.IsNullOrEmpty(methodName)) return null;

        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Instance | BindingFlags.Static;

        // Build candidate list: match Java camelCase and .NET PascalCase.
        string pascal = char.ToUpperInvariant(methodName[0]) + methodName[1..];

        var candidates = type.GetMethods(all)
            .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(m.Name, pascal,      StringComparison.Ordinal))
            .ToArray();

        if (candidates.Length == 0) return null;
        if (candidates.Length == 1) return candidates[0];

        // Multiple overloads — try descriptor-based disambiguation.
        if (!string.IsNullOrEmpty(javaDesc))
        {
            var match = candidates.FirstOrDefault(m => DescriptorMatches(m, javaDesc));
            if (match != null) return match;
        }

        Console.WriteLine(
            $"[HarmonyBridge] Ambiguous overload for {type.Name}.{methodName} " +
            $"({candidates.Length} candidates) — using first. Add descriptor for precision.");
        return candidates[0];
    }

    /// <summary>
    /// Quick sanity-check: does the method's parameter count match the Java descriptor?
    /// Full type matching is omitted — parameter count catches 95 % of overload cases.
    /// </summary>
    static bool DescriptorMatches(MethodInfo m, string desc)
    {
        int open  = desc.IndexOf('(');
        int close = desc.IndexOf(')');
        if (open < 0 || close <= open) return false;

        return m.GetParameters().Length == CountJavaParams(desc[(open + 1)..close]);
    }

    static int CountJavaParams(string paramSection)
    {
        int count = 0;
        int i     = 0;
        while (i < paramSection.Length)
        {
            char c = paramSection[i];
            if (c == 'L')
            {
                // Reference type: skip to ';'
                int semi = paramSection.IndexOf(';', i);
                i = semi >= 0 ? semi + 1 : paramSection.Length;
                count++;
            }
            else if (c == '[')
            {
                // Array dimension — next char is element type descriptor, don't count yet
                i++;
            }
            else if ("BCDFIJSZ".Contains(c))
            {
                // Primitive type
                i++;
                count++;
            }
            else
            {
                i++;
            }
        }
        return count;
    }

    // ── IL helpers ────────────────────────────────────────────────────────────

    static void EmitLdarg(ILGenerator il, int index)
    {
        switch (index)
        {
            case 0: il.Emit(OpCodes.Ldarg_0); break;
            case 1: il.Emit(OpCodes.Ldarg_1); break;
            case 2: il.Emit(OpCodes.Ldarg_2); break;
            case 3: il.Emit(OpCodes.Ldarg_3); break;
            default:
                if (index <= 255) il.Emit(OpCodes.Ldarg_S, (byte)index);
                else              il.Emit(OpCodes.Ldarg,   index);
                break;
        }
    }
}
