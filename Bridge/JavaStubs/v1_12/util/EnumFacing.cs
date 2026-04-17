// AllocGuard Tier 1 — EnumFacing as C# enum (stack, zero GC)
// Java: net.minecraft.util.EnumFacing

namespace net.minecraft.util;

/// <summary>
/// MinecraftStubs v1_12 — EnumFacing.
///
/// The six cardinal directions. Declared as a C# <c>enum</c> so it is
/// stored as an int on the stack — no heap allocation, no boxing in switch.
///
/// Java EnumFacing.ordinal() maps directly to the int value here.
/// </summary>
public enum EnumFacing
{
    DOWN  = 0,
    UP    = 1,
    NORTH = 2,
    SOUTH = 3,
    WEST  = 4,
    EAST  = 5,
}

/// <summary>Extension helpers matching the Java EnumFacing API surface.</summary>
public static class EnumFacingExtensions
{
    private static readonly EnumFacing[] _opposites =
        [EnumFacing.UP, EnumFacing.DOWN, EnumFacing.SOUTH, EnumFacing.NORTH, EnumFacing.EAST, EnumFacing.WEST];

    private static readonly int[] _xOffsets = [0,  0,  0,  0, -1,  1];
    private static readonly int[] _yOffsets = [-1, 1,  0,  0,  0,  0];
    private static readonly int[] _zOffsets = [0,  0, -1,  1,  0,  0];

    private static readonly string[] _names =
        ["down", "up", "north", "south", "west", "east"];

    public static EnumFacing getOpposite(this EnumFacing f) => _opposites[(int)f];
    public static int getFrontOffsetX(this EnumFacing f)    => _xOffsets[(int)f];
    public static int getFrontOffsetY(this EnumFacing f)    => _yOffsets[(int)f];
    public static int getFrontOffsetZ(this EnumFacing f)    => _zOffsets[(int)f];
    public static string getName(this EnumFacing f)         => _names[(int)f];
    public static int getIndex(this EnumFacing f)           => (int)f;
    public static int getHorizontalIndex(this EnumFacing f) => (int)f - 2; // NORTH=0..EAST=3

    public static EnumFacing[] values() =>
        [EnumFacing.DOWN, EnumFacing.UP, EnumFacing.NORTH, EnumFacing.SOUTH, EnumFacing.WEST, EnumFacing.EAST];

    public static EnumFacing byName(string name) =>
        Array.FindIndex(_names, n => n == name.ToLowerInvariant()) is int i && i >= 0
            ? (EnumFacing)i
            : EnumFacing.DOWN;

    public static EnumFacing byIndex(int i) => (EnumFacing)(i % 6);
}
