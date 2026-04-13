namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>aav</c> (MapColor) — 16-slot colour registry for the in-game map display.
/// Each Material holds a MapColor reference. The array has 16 slots; 14 are populated (b–o).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Material_Spec.md §8
/// </summary>
public sealed class MapColor
{
    // ── Static registry ───────────────────────────────────────────────────────

    private static readonly MapColor[] All = new MapColor[16]; // obf: a

    // ── Static instances (obf: b–o, indices 0–13) ─────────────────────────────

    public static readonly MapColor Black      = new(0,  0x000000);   // obf: b — black / transparent
    public static readonly MapColor Grass      = new(1,  0x7FB238);   // obf: c — dark grass green   (8368696)
    public static readonly MapColor Sand       = new(2,  0xF7E9A3);   // obf: d — sand yellow        (16247203)
    public static readonly MapColor Dirt       = new(3,  0xA7A7A7);   // obf: e — dirt brown         (10987431)
    public static readonly MapColor Lava       = new(4,  0xFF0000);   // obf: f — red (lava/redstone)(16711680)
    public static readonly MapColor Ice        = new(5,  0xA0A0FF);   // obf: g — ice blue           (10526975)
    public static readonly MapColor DirtBrown  = new(6,  0xA7A7A7);   // obf: h — dirt brown dup     (10987431)
    public static readonly MapColor LeafGreen  = new(7,  0x007C00);   // obf: i — leaf green         (31744)
    public static readonly MapColor White      = new(8,  0xFFFFFF);   // obf: j — white (snow/ice)   (16777215)
    public static readonly MapColor BlueGrey   = new(9,  0xA4A4A4);   // obf: k — blue-grey          (10791096)
    public static readonly MapColor WoodTan    = new(10, 0xB7A6AF);   // obf: l — wood tan           (12020271)
    public static readonly MapColor StoneGrey  = new(11, 0x707070);   // obf: m — stone grey         (7368816)
    public static readonly MapColor WaterBlue  = new(12, 0x4040FF);   // obf: n — water blue         (4210943)
    public static readonly MapColor Gravel     = new(13, 0x686868);   // obf: o — gravel brown-grey  (6837042)

    // ── Instance fields ───────────────────────────────────────────────────────

    /// <summary>Index into the 16-slot array. obf: q</summary>
    public readonly int Index; // obf: q

    /// <summary>24-bit RGB colour used on the in-game map. obf: p</summary>
    public readonly int Rgb;   // obf: p

    // ── Constructor ───────────────────────────────────────────────────────────

    private MapColor(int index, int rgb)
    {
        Index    = index;
        Rgb      = rgb;
        All[index] = this;
    }
}
