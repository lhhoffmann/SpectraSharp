// Stub for net.minecraft.util.Direction — Minecraft 1.16.5
// 1.16 renamed EnumFacing → Direction.

namespace net.minecraft.util;

/// <summary>
/// MinecraftStubs v1_16 — Direction.
/// Renamed from EnumFacing in 1.12.
/// </summary>
public enum Direction
{
    DOWN  = 0,
    UP    = 1,
    NORTH = 2,
    SOUTH = 3,
    WEST  = 4,
    EAST  = 5,
}

public static class DirectionExtensions
{
    static readonly Direction[] _opposites =
        [Direction.UP, Direction.DOWN, Direction.SOUTH, Direction.NORTH, Direction.EAST, Direction.WEST];
    static readonly int[] _xOff = [0,  0,  0,  0, -1,  1];
    static readonly int[] _yOff = [-1, 1,  0,  0,  0,  0];
    static readonly int[] _zOff = [0,  0, -1,  1,  0,  0];
    static readonly string[] _names = ["down", "up", "north", "south", "west", "east"];

    public static Direction getOpposite(this Direction d) => _opposites[(int)d];
    public static int getStepX(this Direction d)          => _xOff[(int)d];
    public static int getStepY(this Direction d)          => _yOff[(int)d];
    public static int getStepZ(this Direction d)          => _zOff[(int)d];
    public static string getName(this Direction d)        => _names[(int)d];

    public static Direction[] values() =>
        [Direction.DOWN, Direction.UP, Direction.NORTH, Direction.SOUTH, Direction.WEST, Direction.EAST];

    public static Direction byName(string name) =>
        Array.FindIndex(_names, n => n == name.ToLowerInvariant()) is int i && i >= 0
            ? (Direction)i : Direction.DOWN;
}
