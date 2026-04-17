// AllocGuard Tier 1 — BlockPos as stack-allocated value type
// Java: net.minecraft.util.math.BlockPos (introduced in 1.8)
//
// DESIGN: Java BlockPos is a heap-allocated immutable object used for EVERY
//         block coordinate. Mods create millions per tick for iteration, pathfinding,
//         world generation, and event handling. As a readonly record struct,
//         all transient calculations are stack-allocated — zero GC pressure.
//
//         Java BlockPos also packs to a long (toLong/fromLong) for hash keys.
//         The struct preserves this API exactly.

namespace net.minecraft.util.math;

/// <summary>
/// MinecraftStubs v1_12 — BlockPos (net.minecraft.util.math.BlockPos).
///
/// Immutable integer triple representing a block-grid position.
/// Declared as <c>readonly record struct</c> — stack-allocated, zero GC.
///
/// Value equality is correct for positions: two BlockPos are equal iff
/// x, y, z all match. This matches Java semantics (BlockPos.equals()).
/// </summary>
public readonly record struct BlockPos(int X, int Y, int Z)
{
    // ── Neighbours ────────────────────────────────────────────────────────────

    public BlockPos up()         => this with { Y = Y + 1 };
    public BlockPos down()       => this with { Y = Y - 1 };
    public BlockPos north()      => this with { Z = Z - 1 };
    public BlockPos south()      => this with { Z = Z + 1 };
    public BlockPos east()       => this with { X = X + 1 };
    public BlockPos west()       => this with { X = X - 1 };

    public BlockPos up(int n)    => this with { Y = Y + n };
    public BlockPos down(int n)  => this with { Y = Y - n };

    public BlockPos offset(EnumFacing facing) => facing switch
    {
        EnumFacing.UP    => up(),
        EnumFacing.DOWN  => down(),
        EnumFacing.NORTH => north(),
        EnumFacing.SOUTH => south(),
        EnumFacing.EAST  => east(),
        EnumFacing.WEST  => west(),
        _                => this,
    };

    // ── Packing / unpacking (used as HashMap keys in vanilla and mods) ────────

    /// <summary>
    /// Packs this position into a long.
    /// Format: X[37:12] | Z[11:0, sign-extended] | Y[63:38]
    /// Matches vanilla BlockPos.toLong() exactly.
    /// </summary>
    public long toLong() =>
        ((long)(X & 0x3FFFFFF) << 38) |
        ((long)(Z & 0x3FFFFFF) << 12) |
        ((long)(Y & 0xFFF));

    /// <summary>Unpacks a long produced by <see cref="toLong"/>.</summary>
    public static BlockPos fromLong(long packed)
    {
        int x = (int)(packed >> 38);
        int z = (int)(packed << 26 >> 38);
        int y = (int)(packed << 52 >> 52);
        return new BlockPos(x, y, z);
    }

    // ── Distance / comparison ────────────────────────────────────────────────

    public double distanceSq(double x, double y, double z)
    {
        double dx = X - x;
        double dy = Y - y;
        double dz = Z - z;
        return dx * dx + dy * dy + dz * dz;
    }

    public double distanceSqToCenter(double x, double y, double z)
        => distanceSq(x + 0.5, y + 0.5, z + 0.5);

    // ── Arithmetic ────────────────────────────────────────────────────────────

    public BlockPos add(int x, int y, int z) => new(X + x, Y + y, Z + z);
    public BlockPos add(BlockPos other)       => new(X + other.X, Y + other.Y, Z + other.Z);
    public BlockPos subtract(BlockPos other)  => new(X - other.X, Y - other.Y, Z - other.Z);

    // ── Java field aliases (mod code accesses .x .y .z lowercase) ────────────

    public int x => X;
    public int y => Y;
    public int z => Z;

    // ── Getters (Forge mods use getX(), getY(), getZ()) ──────────────────────

    public int getX() => X;
    public int getY() => Y;
    public int getZ() => Z;

    // ── Factory ───────────────────────────────────────────────────────────────

    /// <summary>Creates a mutable MutableBlockPos (returned as a value type copy here).</summary>
    public static BlockPos ORIGIN => new(0, 0, 0);

    // ── Java interop ──────────────────────────────────────────────────────────

    public override string ToString() => $"({X}, {Y}, {Z})";
}

/// <summary>
/// Mutable variant — same as BlockPos but mods can mutate it in loops.
/// Used by vanilla for iteration patterns like filling a cube region.
/// Still a struct to stay on the stack; mutation returns void.
/// </summary>
public record struct MutableBlockPos(int X, int Y, int Z)
{
    public MutableBlockPos setPos(int x, int y, int z) { X = x; Y = y; Z = z; return this; }
    public MutableBlockPos move(EnumFacing facing)
    {
        var next = ((BlockPos)this).offset(facing);
        X = next.X; Y = next.Y; Z = next.Z;
        return this;
    }

    public int getX() => X;
    public int getY() => Y;
    public int getZ() => Z;
    public int x => X;
    public int y => Y;
    public int z => Z;

    public BlockPos toImmutable() => new(X, Y, Z);
    public static implicit operator BlockPos(MutableBlockPos m) => new(m.X, m.Y, m.Z);
}
