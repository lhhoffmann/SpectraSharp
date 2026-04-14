namespace SpectraSharp.Core;

/// <summary>
/// Player abilities flags (creative mode etc.). Replica of <c>wq</c> (PlayerAbilities).
///
/// Persisted as a nested "abilities" TAG_Compound inside the player compound.
///
/// Bug preserved (spec §7.2 / §8.2):
///   The write method writes field <c>a</c> (Invulnerable) for BOTH "invulnerable"
///   AND "flying". The correct field for "flying" is <c>b</c> (IsFlying), but the
///   original code uses <c>a</c> for both — a vanilla 1.0 bug. This means flying
///   state is lost on every save/load unless the player is also invulnerable.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/PlayerNBT_Spec.md §7
/// </summary>
public sealed class PlayerAbilities
{
    // ── Fields (spec §7.1) ────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — invulnerable (immune to damage). Default false.</summary>
    public bool Invulnerable;

    /// <summary>obf: <c>b</c> — currently flying. Default false.</summary>
    public bool IsFlying;

    /// <summary>obf: <c>c</c> — mayfly (allowed to toggle flight). Default false.</summary>
    public bool MayFly;

    /// <summary>obf: <c>d</c> — instabuild (instant block break, creative). Default false.</summary>
    public bool InstaBuild;

    // ── NBT (spec §7.2 / §7.3) ───────────────────────────────────────────────

    /// <summary>
    /// Writes an "abilities" compound into <paramref name="tag"/>.
    /// Spec: <c>wq.a(ik rootTag)</c>.
    ///
    /// Bug: "flying" is written with field <c>a</c> (Invulnerable), not <c>b</c> (IsFlying).
    /// </summary>
    public void WriteToNbt(Nbt.NbtCompound tag)
    {
        var ab = new Nbt.NbtCompound();
        ab.PutBoolean("invulnerable", Invulnerable);
        ab.PutBoolean("flying",       Invulnerable); // BUG: writes 'a' (invulnerable), not 'b' (flying)
        ab.PutBoolean("mayfly",       MayFly);
        ab.PutBoolean("instabuild",   InstaBuild);
        tag.PutCompound("abilities",  ab);
    }

    /// <summary>
    /// Reads from the "abilities" compound inside <paramref name="tag"/>.
    /// Caller must check HasKey("abilities") before calling.
    /// Spec: <c>wq.b(ik rootTag)</c>.
    /// </summary>
    public void ReadFromNbt(Nbt.NbtCompound tag)
    {
        Nbt.NbtCompound? ab = tag.GetCompound("abilities");
        if (ab == null) return;
        Invulnerable = ab.GetBoolean("invulnerable");
        IsFlying     = ab.GetBoolean("flying");
        MayFly       = ab.GetBoolean("mayfly");
        InstaBuild   = ab.GetBoolean("instabuild");
    }
}
