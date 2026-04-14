using System.Reflection;

namespace SpectraSharp.ModRuntime.Interop;

/// <summary>
/// Scans an IKVM-compiled assembly for Mixin annotations.
///
/// IKVM preserves Java annotations as .NET custom attributes.  The SpongePowered Mixin
/// library uses annotations like @Mixin, @Inject, @Overwrite, @Redirect, @Accessor, and
/// @Invoker.  After ikvmc translation these appear as custom attributes whose full type
/// names contain those keywords — we match by name since we do not reference the
/// SpongePowered JARs at runtime.
///
/// Returned <see cref="MixinDescriptor"/> list is empty for mods that use no Mixins
/// (e.g. pure 1.0 ModLoader mods).
/// </summary>
public static class MixinScanner
{
    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all @Mixin descriptors found in <paramref name="asm"/>.
    /// </summary>
    public static List<MixinDescriptor> Scan(Assembly asm)
    {
        var result = new List<MixinDescriptor>();

        foreach (var type in asm.GetTypes())
        {
            string? target = FindMixinTarget(type);
            if (target == null) continue;

            var injections = ScanInjections(type);

            result.Add(new MixinDescriptor
            {
                TargetJavaClass = target,
                Injections      = injections,
                MixinType       = type,
            });
        }

        return result;
    }

    // ── Mixin target resolution ───────────────────────────────────────────────

    /// <summary>
    /// Looks for an attribute whose type name contains "Mixin" and extracts the
    /// target class name from its constructor or named arguments.
    /// Returns null if this type is not a Mixin class.
    /// </summary>
    static string? FindMixinTarget(Type type)
    {
        foreach (var attr in type.GetCustomAttributesData())
        {
            string attrFqn = attr.AttributeType.FullName ?? "";
            if (!attrFqn.Contains("Mixin")) continue;

            // Constructor arg: @Mixin(World.class) → ConstructorArguments[0] = Type
            //                  @Mixin("net.minecraft.world.World") → ConstructorArguments[0] = string
            foreach (var arg in attr.ConstructorArguments)
            {
                switch (arg.Value)
                {
                    case Type t:
                        return JavaNameOf(t);
                    case string s when s.Length > 0:
                        return s.Replace('/', '.');
                    case System.Collections.Generic.IList<CustomAttributeTypedArgument> list:
                        // @Mixin(value = { World.class })
                        if (list.Count > 0 && list[0].Value is Type t2)
                            return JavaNameOf(t2);
                        break;
                }
            }

            // Named arg: @Mixin(value = World.class) or @Mixin(targets = "net/minecraft/world/World")
            foreach (var named in attr.NamedArguments)
            {
                switch (named.MemberName)
                {
                    case "value" when named.TypedValue.Value is Type t:
                        return JavaNameOf(t);
                    case "targets" when named.TypedValue.Value is string s:
                        return s.Replace('/', '.');
                    case "value" when named.TypedValue.Value is
                        System.Collections.Generic.IList<CustomAttributeTypedArgument> list:
                        if (list.Count > 0 && list[0].Value is Type t3)
                            return JavaNameOf(t3);
                        break;
                }
            }
        }

        return null;
    }

    static string JavaNameOf(Type t) =>
        (t.FullName ?? t.Name).Replace('+', '.').Replace('/', '.');

    // ── Injection scanning ────────────────────────────────────────────────────

    static List<MixinInjection> ScanInjections(Type type)
    {
        var list = new List<MixinInjection>();
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Instance | BindingFlags.Static |
                                 BindingFlags.DeclaredOnly;

        foreach (var method in type.GetMethods(all))
        {
            var inj = InspectMethod(method);
            if (inj != null) list.Add(inj);
        }

        return list;
    }

    static MixinInjection? InspectMethod(MethodInfo method)
    {
        foreach (var attr in method.GetCustomAttributesData())
        {
            string name = attr.AttributeType.FullName ?? "";

            if      (name.Contains("Inject"))    return BuildInject(method, attr);
            else if (name.Contains("Overwrite"))  return BuildOverwrite(method);
            else if (name.Contains("Redirect"))   return BuildRedirect(method, attr);
            else if (name.Contains("Accessor"))   return new MixinInjection { Kind = InjectionKind.Accessor, MixinMethod = method, TargetMethod = method.Name };
            else if (name.Contains("Invoker"))    return new MixinInjection { Kind = InjectionKind.Invoker,  MixinMethod = method, TargetMethod = method.Name };
        }

        return null;
    }

    // ── @Inject ───────────────────────────────────────────────────────────────

    static MixinInjection BuildInject(MethodInfo method, CustomAttributeData attr)
    {
        string       targetMethod = "";
        InjectionAt  at           = InjectionAt.Head;
        string       desc         = "";

        foreach (var named in attr.NamedArguments)
        {
            switch (named.MemberName)
            {
                case "method":
                    targetMethod = ExtractString(named.TypedValue) ?? method.Name;
                    break;
                case "at":
                    at = ParseAt(named.TypedValue);
                    break;
                case "desc" or "descriptor":
                    desc = ExtractString(named.TypedValue) ?? "";
                    break;
            }
        }

        // Constructor args: @Inject(method="foo", at=@At("HEAD"))
        // IKVM may flatten nested @At into a string
        if (attr.ConstructorArguments.Count >= 1)
            targetMethod = ExtractString(attr.ConstructorArguments[0]) ?? targetMethod;
        if (attr.ConstructorArguments.Count >= 2)
            at = ParseAtString(ExtractString(attr.ConstructorArguments[1]) ?? "HEAD");

        return new MixinInjection
        {
            Kind         = InjectionKind.Inject,
            At           = at,
            TargetMethod = targetMethod,
            TargetDesc   = desc,
            MixinMethod  = method,
        };
    }

    static InjectionAt ParseAt(CustomAttributeTypedArgument arg)
    {
        string raw = ExtractString(arg) ?? "";
        return ParseAtString(raw);
    }

    static InjectionAt ParseAtString(string raw) =>
        raw.ToUpperInvariant() switch
        {
            "HEAD"   => InjectionAt.Head,
            "RETURN" => InjectionAt.Return,
            "INVOKE" => InjectionAt.Invoke,
            "TAIL"   => InjectionAt.Tail,
            _        => InjectionAt.Head,
        };

    // ── @Overwrite ────────────────────────────────────────────────────────────

    static MixinInjection BuildOverwrite(MethodInfo method) =>
        new()
        {
            Kind         = InjectionKind.Overwrite,
            TargetMethod = method.Name,
            MixinMethod  = method,
        };

    // ── @Redirect ─────────────────────────────────────────────────────────────

    static MixinInjection BuildRedirect(MethodInfo method, CustomAttributeData attr)
    {
        string targetMethod  = "";
        string redirectAt    = "";

        foreach (var named in attr.NamedArguments)
        {
            switch (named.MemberName)
            {
                case "method": targetMethod = ExtractString(named.TypedValue) ?? ""; break;
                case "at":     redirectAt   = ExtractString(named.TypedValue) ?? ""; break;
            }
        }

        return new MixinInjection
        {
            Kind           = InjectionKind.Redirect,
            TargetMethod   = targetMethod,
            RedirectTarget = redirectAt,
            MixinMethod    = method,
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string? ExtractString(CustomAttributeTypedArgument arg)
    {
        if (arg.Value is string s) return s;
        if (arg.Value is Type t)   return JavaNameOf(t);
        return null;
    }
}
