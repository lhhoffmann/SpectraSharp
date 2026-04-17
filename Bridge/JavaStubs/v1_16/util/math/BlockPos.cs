// Stub for net.minecraft.util.math.BlockPos — Minecraft 1.16.5
// API unchanged from 1.12; still in net.minecraft.util.math package.

namespace net.minecraft.util.math;

/// <summary>
/// MinecraftStubs v1_16 — BlockPos.
/// Immutable integer triple. Declared as readonly record struct for zero GC.
/// </summary>
public readonly record struct BlockPos(int X, int Y, int Z)
{
    public BlockPos up()    => this with { Y = Y + 1 };
    public BlockPos down()  => this with { Y = Y - 1 };
    public BlockPos north() => this with { Z = Z - 1 };
    public BlockPos south() => this with { Z = Z + 1 };
    public BlockPos east()  => this with { X = X + 1 };
    public BlockPos west()  => this with { X = X - 1 };

    public BlockPos above()  => up();
    public BlockPos below()  => down();

    public BlockPos up(int n)   => this with { Y = Y + n };
    public BlockPos down(int n) => this with { Y = Y - n };

    public BlockPos offset(net.minecraft.util.Direction facing) => facing switch
    {
        net.minecraft.util.Direction.UP    => up(),
        net.minecraft.util.Direction.DOWN  => down(),
        net.minecraft.util.Direction.NORTH => north(),
        net.minecraft.util.Direction.SOUTH => south(),
        net.minecraft.util.Direction.EAST  => east(),
        net.minecraft.util.Direction.WEST  => west(),
        _                                  => this,
    };

    public long toLong() =>
        ((long)(X & 0x3FFFFFF) << 38) |
        ((long)(Z & 0x3FFFFFF) << 12) |
        ((long)(Y & 0xFFF));

    public static BlockPos fromLong(long packed)
    {
        int x = (int)(packed >> 38);
        int z = (int)(packed << 26 >> 38);
        int y = (int)(packed << 52 >> 52);
        return new BlockPos(x, y, z);
    }

    public double distanceSq(double x, double y, double z)
    {
        double dx = X - x; double dy = Y - y; double dz = Z - z;
        return dx * dx + dy * dy + dz * dz;
    }

    public BlockPos add(int x, int y, int z)    => new(X + x, Y + y, Z + z);
    public BlockPos add(BlockPos other)          => new(X + other.X, Y + other.Y, Z + other.Z);
    public BlockPos subtract(BlockPos other)     => new(X - other.X, Y - other.Y, Z - other.Z);

    public int x => X;
    public int y => Y;
    public int z => Z;

    public int getX() => X;
    public int getY() => Y;
    public int getZ() => Z;

    public static BlockPos ORIGIN => new(0, 0, 0);

    public override string ToString() => $"({X}, {Y}, {Z})";
}
