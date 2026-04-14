namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>vi</c> (EntityPlayer) — abstract base for all player-controlled entities.
/// Extends <see cref="LivingEntity"/> (<c>nq</c>).
///
/// Concrete subclasses: server-side player, client-side player (pending).
///
/// Key overrides from LivingEntity:
///   Eye height = 1.62F, AABB 0.6 wide × 1.8 tall, maxHealth = 20.
///   CanDespawnNaturally() = false (players never naturally despawn).
///   GetEquippedItem() delegates to Inventory.GetStackInSelectedSlot().
///
/// Open stubs:
///   - Food stats (eq): FoodStats field not yet implemented.
///   - Container screen (pj/gd): OpenContainer not yet implemented.
///   - getMiningSpeed: enchantment and potion modifiers skipped.
///   - Sleeping, dimension, cloak URL fields: declared but unused.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityPlayer_Spec.md
/// </summary>
public abstract class EntityPlayer : LivingEntity
{
    // ── Eye height (spec §4, §5) ──────────────────────────────────────────────

    /// <summary>
    /// obf: <c>L</c> — eye height above feet position.
    /// Default 1.62F. Reset to 0.1F on death, restored to 1.62F on respawn.
    /// Note: Entity.YOffset (also obf L) is a different field; this is player-specific.
    /// </summary>
    public float PlayerEyeHeight = 1.62f;

    // ── Inventory (spec §2) ───────────────────────────────────────────────────

    /// <summary>obf: <c>by</c> — the player's personal inventory.</summary>
    public readonly InventoryPlayer Inventory;

    // ── Player-specific fields (spec §2) ──────────────────────────────────────

    /// <summary>obf: <c>bC</c> — cooldown counter for various actions.</summary>
    public int ActionCooldown;

    /// <summary>obf: <c>bE</c> — score (total XP gained, damage dealt, etc.).</summary>
    public int Score;

    /// <summary>obf: <c>bH</c> — isUsingItem flag.</summary>
    public bool IsUsingItem;

    /// <summary>obf: <c>bI</c> — number of ticks the current item has been in use.</summary>
    public int ItemInUseCount;

    /// <summary>obf: <c>bJ</c> — player username.</summary>
    public string PlayerName = string.Empty;

    /// <summary>obf: <c>bK</c> — dimension ID (0 = Overworld, -1 = Nether, 1 = End).</summary>
    public int Dimension;

    /// <summary>obf: <c>bZ</c> — sneaking flag (driven by client input).</summary>
    public bool IsSneaking;

    // ── NBT-persisted player fields (spec: PlayerNBT_Spec §5.4) ──────────────

    /// <summary>obf: <c>bT</c> — whether player is sleeping in a bed.</summary>
    public bool IsSleeping;

    /// <summary>obf: <c>a</c> (on vi, not Entity.a) — ticks spent sleeping (target: 100).</summary>
    public int SleepTimer;

    /// <summary>obf: <c>cf</c> — XP progress within current level (0.0–1.0).</summary>
    public float XpProgress;

    /// <summary>obf: <c>cd</c> — current XP level.</summary>
    public int XpLevel;

    /// <summary>obf: <c>ce</c> — total XP accumulated.</summary>
    public int XpTotal;

    /// <summary>
    /// Bed-respawn coordinates. obf: <c>b</c> (a ChunkCoordinates). Null = no bed spawn.
    /// Only written to NBT when non-null (spec §8.3).
    /// </summary>
    public (int x, int y, int z)? BedSpawn;

    /// <summary>obf: <c>bB</c> — food stats.</summary>
    public readonly FoodStats FoodStats = new();

    /// <summary>obf: <c>cc</c> — player abilities (creative mode flags etc.).</summary>
    public readonly PlayerAbilities Abilities = new();

    // ── Constructor (spec §4) ─────────────────────────────────────────────────

    protected EntityPlayer(World world) : base(world)
    {
        Inventory   = new InventoryPlayer(this);
        TexturePath = "/mob/char.png";   // obf: aA
        SetSize(0.6f, 1.8f);             // obf: a(0.6F, 1.8F) — width × height
    }

    // ── EntityInit override (spec §7) ─────────────────────────────────────────

    /// <summary>
    /// Registers DataWatcher indices 16 and 17 (player-specific flags).
    /// Spec: <c>b()</c> in vi.
    /// </summary>
    protected override void EntityInit()
    {
        base.EntityInit(); // registers index 8 (potion color)
        DataWatcher.Register(16, (byte)0);
        DataWatcher.Register(17, (byte)0);
    }

    // ── MaxHealth (spec §6) ───────────────────────────────────────────────────

    /// <summary>obf: <c>f_()</c> — player max health is always 20 (10 hearts).</summary>
    public override int GetMaxHealth() => 20;

    // ── Respawn / reset (spec §5) ─────────────────────────────────────────────

    /// <summary>
    /// obf: <c>u()</c> — called on respawn. Resets size, eye height, health, death timer.
    /// </summary>
    public virtual void Respawn()
    {
        PlayerEyeHeight = 1.62f;
        SetSize(0.6f, 1.8f);
        SetHealth(GetMaxHealth());
        DeathTime = 0;
    }

    // ── LivingEntity overrides ────────────────────────────────────────────────

    /// <summary>Players never naturally despawn.</summary>
    public override bool CanDespawnNaturally() => false;

    /// <summary>Returns the currently held item from the hotbar.</summary>
    public override ItemStack? GetEquippedItem() => Inventory.GetStackInSelectedSlot();

    // ── getMiningSpeed (spec §8) ──────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(yy block)</c> — returns effective dig speed against the given block.
    /// Enchantment (Efficiency) and potion (Haste/Fatigue) modifiers are stubs.
    /// Water-penalty and air-penalty are applied.
    /// </summary>
    public float GetMiningSpeed(Block block)
    {
        ItemStack? held = Inventory.GetStackInSelectedSlot();
        float speed = held != null
            ? Item.ItemsList[held.ItemId]?.GetMiningSpeed(held, block) ?? 1.0f
            : 1.0f;

        // Water penalty: if in water and not holding a waterproof tool (stub: always penalise)
        if (IsInWater) speed /= 5.0f;

        // Air penalty: not on ground
        if (!OnGround) speed /= 5.0f;

        return speed;
    }

    // ── Death behaviour (spec §10) ────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(pm cause)</c> — on death: shrink AABB, drop all inventory, set eye height.
    /// </summary>
    protected override void OnDeath(DamageSource damageSource)
    {
        base.OnDeath(damageSource);
        SetSize(0.2f, 0.2f);
        MotionY = 0.1f;
        Inventory.DropAllItems();
        PlayerEyeHeight = 0.1f;
    }

    // ── DropItem (spec §9) ────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>a(dk stack, boolean randomDirection)</c> — spawns an EntityItem near the player.
    /// Pickup delay = 40 ticks (2 seconds).
    /// </summary>
    public void DropItem(ItemStack stack, bool randomDirection)
    {
        if (World == null) return;

        // Spawn at eye height: posY + eyeHeight - 0.3F
        double spawnY = PosY + PlayerEyeHeight - 0.30;

        var entity = new EntityItem(World, PosX, spawnY, PosZ, stack);
        entity.PickupDelay = 40;

        if (randomDirection)
        {
            entity.MotionX = EntityRandom.NextFloat() * 0.02f;
            entity.MotionY = 0.1f;
            entity.MotionZ = EntityRandom.NextFloat() * 0.02f;
        }
        else
        {
            float yawRad   = RotationYaw   * MathF.PI / 180.0f;
            float pitchRad = RotationPitch * MathF.PI / 180.0f;
            entity.MotionX = -MathHelper.Sin(yawRad) * MathHelper.Cos(pitchRad) * 0.3f;
            entity.MotionZ =  MathHelper.Cos(yawRad) * MathHelper.Cos(pitchRad) * 0.3f;
            entity.MotionY = -MathHelper.Sin(pitchRad) * 0.3f + 0.1f;
        }

        World.SpawnEntity(entity);
    }

    // ── Tick override ─────────────────────────────────────────────────────────

    /// <summary>
    /// Player tick: calls base living tick, then decrements inventory animations.
    /// </summary>
    public override void Tick()
    {
        base.Tick();
        Inventory.DecrementAnimations();

        if (ActionCooldown > 0) ActionCooldown--;

        // Food tick: runs server-side only (spec: ItemFood_Spec §5.3)
        if (World != null && !World.IsClientSide)
            FoodStats.Tick(this);
    }

    // ── Item-use mechanics ────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>c(dk stack, int duration)</c> — starts the use-item countdown (eat/drink/bow).
    /// Stub: sets the active item and countdown counter. Full use-tick logic pending.
    /// </summary>
    public void StartUsingItem(ItemStack stack, int duration)
    {
        // Stub — full ItemInUse countdown not yet implemented.
    }

    // ── Placement reachability stub (spec §4 guard 2) ─────────────────────────

    /// <summary>
    /// obf: <c>a_(ry, int, int, int)</c> — returns true if the player can place a block
    /// at the given position (build-height check and chunk-load check).
    /// Stub: always returns true (height limit and chunk loading not enforced here).
    /// </summary>
    public virtual bool CanPlaceBlockAt(IWorld world, int x, int y, int z) => true;

    // ── NBT stubs (ik spec pending) ───────────────────────────────────────────

    // ── PlayerNBT write / read (spec: PlayerNBT_Spec) ────────────────────────

    /// <summary>
    /// Writes player-specific NBT fields. Spec: <c>vi.a(ik tag)</c>.
    /// Called from the <see cref="Entity.SaveToNbt"/> dispatch chain after LivingEntity writes
    /// its fields (this overrides LivingEntity's WriteEntityToNBT).
    /// </summary>
    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        // LivingEntity fields first (Health / HurtTime / DeathTime / AttackTime)
        base.WriteEntityToNBT(tag);

        // vi.a: player-specific fields
        tag.PutList("Inventory", Inventory.WriteToNbt());
        tag.PutInt("Dimension",  Dimension);
        tag.PutBoolean("Sleeping",  IsSleeping);
        tag.PutShort("SleepTimer",  (short)SleepTimer);
        tag.PutFloat("XpP",      XpProgress);
        tag.PutInt("XpLevel",    XpLevel);
        tag.PutInt("XpTotal",    XpTotal);

        if (BedSpawn.HasValue)
        {
            tag.PutInt("SpawnX", BedSpawn.Value.x);
            tag.PutInt("SpawnY", BedSpawn.Value.y);
            tag.PutInt("SpawnZ", BedSpawn.Value.z);
        }

        Abilities.WriteToNbt(tag);
        FoodStats.WriteToNbt(tag);
    }

    /// <summary>
    /// Reads player-specific NBT fields. Spec: <c>vi.b(ik tag)</c>.
    /// Called from the <see cref="Entity.LoadFromNbt"/> dispatch chain after LivingEntity reads.
    /// </summary>
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        // LivingEntity fields first
        base.ReadEntityFromNBT(tag);

        // vi.b: player-specific fields
        Inventory.ReadFromNbt(tag.GetList("Inventory"));
        Dimension   = tag.GetInt("Dimension");
        IsSleeping  = tag.GetBoolean("Sleeping");
        SleepTimer  = tag.GetShort("SleepTimer");
        XpProgress  = tag.GetFloat("XpP");
        XpLevel     = tag.GetInt("XpLevel");
        XpTotal     = tag.GetInt("XpTotal");

        // Bed spawn — only set if all three keys present (spec §8.3)
        if (tag.HasKey("SpawnX") && tag.HasKey("SpawnY") && tag.HasKey("SpawnZ"))
            BedSpawn = (tag.GetInt("SpawnX"), tag.GetInt("SpawnY"), tag.GetInt("SpawnZ"));

        if (tag.HasKey("abilities"))
            Abilities.ReadFromNbt(tag);

        if (tag.HasKey("foodLevel"))
            FoodStats.ReadFromNbt(tag);
    }
}
