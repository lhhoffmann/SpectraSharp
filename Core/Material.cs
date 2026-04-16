namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>p</c> (Material) — physical properties of a block substance.
///
/// Note: The obfuscated name was previously misidentified as <c>wu</c> in classes.md.
/// Corrected: <c>p</c> = Material, <c>wu</c> = StepSound. See StepSound_Spec.md §0.
///
/// Quirks preserved (see spec §11):
///   1. GetMobility() (l()) doubles as the light-opacity source in Block.GetLightOpacity().
///      Immovable blocks (J=1) return light opacity 1; push-destroys (J=2) return 2.
///   2. BlocksMovement() always returns true in the base class; only air (slot 0) is
///      flagged passable explicitly.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Material_Spec.md
/// </summary>
public class Material
{
    // ── Instance fields (spec §2) ─────────────────────────────────────────────

    public readonly MapColor MapColor;   // obf: E
    private bool   _isFlammable;         // obf: F  set by f()
    private bool   _isReplaceable;       // obf: G  set by h()
    private bool   _noCollision;         // obf: H  set by private o()
    private bool   _blocksLight = true;  // obf: I  set to false by e()
    private int    _mobility;            // obf: J  0=pushable, 1=immovable, 2=push-destroys

    // ── Constructor (spec §4) ─────────────────────────────────────────────────

    public Material(MapColor mapColor)
    {
        MapColor = mapColor;
    }

    // ── Builder methods (spec §5) — all return this ───────────────────────────

    /// <summary>obf: e() — sets I=false (transparent, does not block light).</summary>
    protected Material SetTransparent() { _blocksLight = false; return this; }

    /// <summary>obf: f() — sets F=true (flammable).</summary>
    protected Material SetFlammable() { _isFlammable = true; return this; }

    /// <summary>obf: h() — sets G=true (replaceable). public in Java.</summary>
    public    Material SetReplaceable() { _isReplaceable = true; return this; }

    /// <summary>obf: m() — sets J=1 (immovable by pistons).</summary>
    protected Material SetImmovable() { _mobility = 1; return this; }

    /// <summary>obf: n() — sets J=2 (push-destroys: piston breaks the block).</summary>
    protected Material SetPushDestroys() { _mobility = 2; return this; }

    /// <summary>obf: private o() — sets H=true (no collision, passable).</summary>
    protected Material SetNoCollision() { _noCollision = true; return this; }

    // ── Methods (spec §6) ─────────────────────────────────────────────────────

    /// <summary>obf: a() — false in base class. Overridden by MaterialLiquid to return true.</summary>
    public virtual bool IsLiquid() => false;

    /// <summary>obf: b() — true in base class. Used in Block.isNormalCube path.</summary>
    public virtual bool IsSolid() => true;

    /// <summary>
    /// obf: c() — true in base class. Used in Block constructor:
    /// <c>CanPassThrough[blockId] = !material.BlocksMovement()</c>.
    /// Default true → CanPassThrough = false for all solid blocks (quirk 2).
    /// </summary>
    public virtual bool BlocksMovement() => true;

    /// <summary>obf: d() — true in base class. Used by CanBePushed.</summary>
    protected virtual bool D() => true;

    /// <summary>obf: g() — true if flammable.</summary>
    public bool IsBurnable() => _isFlammable;

    /// <summary>obf: i() — true if blocks with this material can be replaced by placing.</summary>
    public bool IsReplaceable() => _isReplaceable;

    /// <summary>obf: j() — false if passable (H=true), else delegates to D().</summary>
    public bool CanBePushed() => !_noCollision && D();

    /// <summary>obf: k() — true if this material blocks light (default true).</summary>
    public bool BlocksLight() => _blocksLight;

    /// <summary>
    /// obf: l() — returns J: 0=pushable, 1=immovable, 2=push-destroys.
    /// Also returned as light-opacity value in Block.GetLightOpacity() (quirk 1).
    /// </summary>
    public int GetMobility() => _mobility;

    // ── Static material instances (spec §3) — defined on Material, not on Block ──
    //    Obfuscated names: a–D (31 instances total; A–D are capitalised to avoid keyword clash)

    public static readonly Material Air           = new MaterialPassable(MapColor.Black);                         // a (br subclass)
    public static readonly Material Grass_        = new Material(MapColor.Grass);                                 // b
    public static readonly Material Ground        = new Material(MapColor.WoodTan);                               // c
    public static readonly Material Plants        = new Material(MapColor.LeafGreen).SetFlammable();              // d — flammable
    public static readonly Material RockTransp    = new Material(MapColor.StoneGrey).SetTransparent();           // e — I=false
    public static readonly Material RockTransp2   = new Material(MapColor.Gravel).SetTransparent();              // f — I=false
    public static readonly Material Water         = new MaterialLiquid(MapColor.WaterBlue).SetImmovable();        // g — liquid
    public static readonly Material Lava_         = new MaterialLiquid(MapColor.Lava).SetImmovable();            // h — liquid
    public static readonly Material Leaves        = new Material(MapColor.LeafGreen)
                                                        .SetFlammable().SetNoCollision().SetImmovable();          // i
    public static readonly Material WebMat_J      = new MaterialWeb(MapColor.LeafGreen).SetImmovable();          // j
    public static readonly Material Vine          = new MaterialWeb(MapColor.LeafGreen)
                                                        .SetFlammable().SetImmovable().SetReplaceable();          // k
    public static readonly Material Mat_L         = new Material(MapColor.DirtBrown);                            // l
    public static readonly Material Mat_M         = new Material(MapColor.DirtBrown).SetFlammable();             // m
    public static readonly Material Portal_N      = new MaterialPassable(MapColor.Black).SetImmovable();         // n (br subclass)
    public static readonly Material Mat_O         = new Material(MapColor.Sand);                                  // o
    public static readonly Material MatWeb_P      = new MaterialWeb(MapColor.Black).SetImmovable();              // p (field)
    public static readonly Material MatPass_Q     = new Material(MapColor.Black).SetNoCollision();               // q — passable
    public static readonly Material Mat_R         = new Material(MapColor.Lava)
                                                        .SetFlammable().SetNoCollision();                         // r
    public static readonly Material Mat_S         = new Material(MapColor.LeafGreen).SetImmovable();             // s
    public static readonly Material Mat_T         = new Material(MapColor.Ice).SetNoCollision();                 // t
    public static readonly Material Snow          = new MaterialWeb(MapColor.White)
                                                        .SetReplaceable().SetNoCollision().SetTransparent()
                                                        .SetImmovable();                                          // u
    public static readonly Material Ice_          = new Material(MapColor.White).SetTransparent();               // v — I=false
    public static readonly Material Mat_W         = new Material(MapColor.LeafGreen)
                                                        .SetNoCollision().SetImmovable();                         // w
    public static readonly Material Mat_X         = new Material(MapColor.BlueGrey);                             // x
    public static readonly Material Mat_Y         = new Material(MapColor.LeafGreen).SetImmovable();             // y
    public static readonly Material Mat_Z         = new Material(MapColor.LeafGreen).SetImmovable();             // z
    public static readonly Material Portal_A      = new MaterialPushDestroys(MapColor.Black);                    // A (bk subclass, J=2)
    public static readonly Material Mat_B         = new Material(MapColor.Black).SetImmovable();                 // B
    public static readonly Material Mat_C         = new MaterialTransparent(MapColor.DirtBrown)
                                                        .SetTransparent().SetImmovable();                         // C (tx subclass)
    public static readonly Material Mat_D         = new Material(MapColor.StoneGrey).SetPushDestroys();          // D — J=2

    // ── Common aliases used by tests and mod stubs ───────────────────────────

    public static readonly Material Passable = MatPass_Q;    // test alias for passable (air-like) material
    public static readonly Material Fire     = Portal_N;     // test alias for fire/portal material
    public static readonly Material Sand     = Mat_O;        // test alias for sand material

    // ── Subclasses (spec §7) ──────────────────────────────────────────────────

    /// <summary>sn — MaterialLiquid: overrides IsLiquid() → true.</summary>
    private sealed class MaterialLiquid(MapColor c) : Material(c)
    {
        public override bool IsLiquid() => true;
        // IsSolid default true; BlocksMovement default true
    }

    /// <summary>mw — unknown subclass. No confirmed overrides.</summary>
    private class MaterialWeb(MapColor c) : Material(c) { }

    /// <summary>br — unknown subclass used for Air/Portal materials.</summary>
    private class MaterialPassable(MapColor c) : Material(c) { }

    /// <summary>bk — push-destroys subclass (J=2 via constructor).</summary>
    private sealed class MaterialPushDestroys(MapColor c) : Material(c)
    {
        // n() called in static field init sets J=2; we invoke it here via builder
        public MaterialPushDestroys SetInit() { SetPushDestroys(); return this; }
    }

    /// <summary>tx — transparent/immovable subclass.</summary>
    private class MaterialTransparent(MapColor c) : Material(c) { }
}
