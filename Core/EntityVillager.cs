namespace SpectraEngine.Core.Mobs;

/// <summary>
/// Replica of <c>ai</c> (EntityVillager) — passive villager entity.
/// Extends <see cref="EntityAI"/> (ww base class). Five professions.
/// No trading in 1.0 — AI behaviour inherited from base.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityVillager_Spec.md
/// </summary>
public sealed class EntityVillager : EntityAI
{
    /// <summary>Profession ID 0–4. obf: <c>a</c>.</summary>
    public int Profession;

    // ── Profession texture paths ──────────────────────────────────────────────
    private static readonly string[] ProfessionTextures =
    [
        "/mob/villager/farmer.png",
        "/mob/villager/librarian.png",
        "/mob/villager/priest.png",
        "/mob/villager/smith.png",
        "/mob/villager/butcher.png",
    ];

    // ── Constructors ──────────────────────────────────────────────────────────

    public EntityVillager(World world, int profession) : base(world)
    {
        Profession  = profession;
        ApplyTexture();
    }

    public EntityVillager(World world) : this(world, 0) { }

    // ── Virtuals ──────────────────────────────────────────────────────────────

    public override int GetMaxHealth() => 20;

    // Villagers do NOT burn in sunlight (not undead). obf: d()
    public bool IsSensitiveToSunlight() => false;

    // ── Texture helper ────────────────────────────────────────────────────────

    // obf: ax() — sets texture path from profession.
    private void ApplyTexture()
    {
        TexturePath = Profession >= 0 && Profession < ProfessionTextures.Length
            ? ProfessionTextures[Profession]
            : "/mob/villager/villager.png";
    }

    // ── Sounds ────────────────────────────────────────────────────────────────

    public override string? GetAmbientSound() => "mob.villager.default";
    public override string  GetHurtSound()   => "mob.villager.defaulthurt";
    public override string  GetDeathSound()  => "mob.villager.defaultdeath";

    // ── NBT ───────────────────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutInt("Profession", Profession);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        Profession = tag.GetInt("Profession");
        ApplyTexture();
    }
}
