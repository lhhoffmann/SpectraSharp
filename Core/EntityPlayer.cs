namespace SpectraEngine.Core;

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

    /// <summary>obf: <c>bU</c> — head-block position of the bed the player is currently sleeping in. Null when awake.</summary>
    public (int x, int y, int z)? BedPosition;

    /// <summary>obf: <c>bV</c> — sleeping pose X offset (set by <see cref="SetSleepingPoseOffset"/>).</summary>
    public float SleepPoseOffsetX;

    /// <summary>obf: <c>bX</c> — sleeping pose Z offset (set by <see cref="SetSleepingPoseOffset"/>).</summary>
    public float SleepPoseOffsetZ;

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

    /// <summary>No-arg constructor for test stubs that do not need a real World reference.</summary>
    protected EntityPlayer() : this(null!) { }

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

    /// <summary>Drops a stack when a container is closed — uses random throw direction.</summary>
    public void DropPlayerItem(ItemStack stack) => DropItem(stack, randomDirection: true);

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

    // ── Chat / notifications ──────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>vi.b(String)</c> — sends a localization key as a chat message to the player.
    /// Stub in base class; concrete server/client player subclasses override to send to client.
    /// </summary>
    public virtual void SendMessage(string messageKey) { /* stub */ }

    // ── Sleep (BlockBed_Spec §12-14) ──────────────────────────────────────────

    /// <summary>
    /// obf: <c>vi.d(int, int, int)</c> — trySleep. Attempts to enter sleep state.
    /// Returns an <see cref="EnumSleepResult"/> describing the outcome.
    /// All checks happen server-side; client always returns Ok immediately.
    /// Spec: BlockBed_Spec §12.
    /// </summary>
    public EnumSleepResult TrySleep(int bedX, int bedY, int bedZ)
    {
        if (World == null) return EnumSleepResult.Ok;

        if (!World.IsClientSide)
        {
            // Already sleeping or dead
            if (IsSleeping || !IsEntityAlive()) return EnumSleepResult.AlreadySleeping;

            // Wrong dimension
            if (World.WorldProvider?.SleepingDisabled == true) return EnumSleepResult.WrongDimension;

            // Not night
            if (World.IsDaytime()) return EnumSleepResult.NotNight;

            // Too far from bed
            if (Math.Abs(PosX - bedX) > 3.0 || Math.Abs(PosY - bedY) > 2.0 || Math.Abs(PosZ - bedZ) > 3.0)
                return EnumSleepResult.TooFar;

            // Monsters nearby (±8 XZ, ±5 Y)
            if (World.HasMonstersNearBed(bedX, bedY, bedZ))
                return EnumSleepResult.NotSafe;
        }

        // Shrink player to sleeping size (spec §12 step 2)
        SetSize(0.2f, 0.2f);

        // Position inside bed based on facing (spec §12 step 3)
        if (World.GetBlockId(bedX, bedY, bedZ) == 26)
        {
            int facing = BlockBed.GetFacing(World.GetBlockMetadata(bedX, bedY, bedZ));
            float offsetX = 0.5f, offsetZ = 0.5f;
            switch (facing)
            {
                case 0: offsetZ = 0.9f; break;
                case 1: offsetX = 0.1f; break;
                case 2: offsetZ = 0.1f; break;
                case 3: offsetX = 0.9f; break;
            }
            SetSleepingPoseOffset(facing);
            SetPosition(bedX + offsetX, bedY + 0.9375, bedZ + offsetZ);
        }
        else
        {
            SetPosition(bedX + 0.5, bedY + 0.9375, bedZ + 0.5);
        }

        IsSleeping   = true;
        SleepTimer   = 0;
        BedPosition  = (bedX, bedY, bedZ);
        MotionX = MotionY = MotionZ = 0.0;

        if (!World.IsClientSide)
            World.CheckAllPlayersSleeping();

        return EnumSleepResult.Ok;
    }

    /// <summary>
    /// obf: <c>vi.a(bool, bool, bool)</c> — wakeUpPlayer. Restores player state after sleeping.
    /// Spec: BlockBed_Spec §14.
    /// </summary>
    public void WakeUpPlayer(bool setSpawn, bool broadcastWake, bool setSpawnpoint)
    {
        // Restore normal player size (spec §14 step 1)
        SetSize(0.6f, 1.8f);
        ResetPositionToBoundingBox();

        var bedPos = BedPosition;

        if (bedPos.HasValue && World != null &&
            World.GetBlockId(bedPos.Value.x, bedPos.Value.y, bedPos.Value.z) == 26)
        {
            // Clear occupied flag (spec §14 step 3a)
            BlockBed.SetOccupied(World, bedPos.Value.x, bedPos.Value.y, bedPos.Value.z, false);

            // Find safe wakeup position (spec §14 step 3b-d)
            var wakePos = BlockBed.FindWakeupPosition(World, bedPos.Value.x, bedPos.Value.y, bedPos.Value.z, 0);
            if (wakePos == null)
                wakePos = (bedPos.Value.x, bedPos.Value.y + 1, bedPos.Value.z);

            SetPosition(wakePos.Value.x + 0.5, wakePos.Value.y + YOffset + 0.1, wakePos.Value.z + 0.5);
        }

        IsSleeping = false;

        if (World != null && !World.IsClientSide && broadcastWake)
            World.CheckAllPlayersSleeping();

        // Sleep counter: 0 = setSpawn path (just woke via natural dawn), 100 = manual wake
        SleepTimer = setSpawn ? 0 : 100;

        if (setSpawnpoint && bedPos.HasValue)
            BedSpawn = bedPos;
    }

    /// <summary>
    /// obf: <c>vi.b(int facing)</c> — sets bV/bX sleeping pose visual offsets.
    /// Spec: BlockBed_Spec §13.
    /// </summary>
    public void SetSleepingPoseOffset(int facing)
    {
        switch (facing)
        {
            case 0: SleepPoseOffsetX =  0.0f; SleepPoseOffsetZ = -1.8f; break; // south
            case 1: SleepPoseOffsetX =  1.8f; SleepPoseOffsetZ =  0.0f; break; // west
            case 2: SleepPoseOffsetX =  0.0f; SleepPoseOffsetZ =  1.8f; break; // north
            case 3: SleepPoseOffsetX = -1.8f; SleepPoseOffsetZ =  0.0f; break; // east
        }
    }

    // ── Portal / dimension travel (BlockPortal_Spec §4, ChunkProviderEnd_Spec §7) ─

    /// <summary>
    /// obf: <c>bY</c> — portal cooldown counter. 20 = initial (first portal entry needs 20 ticks).
    /// Decremented each tick by the server tick loop. While > 0 and in portal: held at 10.
    /// </summary>
    public int PortalCooldown = 20;

    /// <summary>
    /// obf: <c>bZ</c> — portal teleport trigger. Set true when PortalCooldown reaches 0
    /// while the player is inside a portal. Cleared after dimension transfer.
    /// Note: this field name intentionally differs from the sneaking flag (also bZ in spec vi).
    /// In practice, bZ is reused — sneaking is in DataWatcher; portal trigger is bZ here.
    /// </summary>
    public bool PortalTrigger;

    /// <summary>
    /// obf: <c>S()</c> — inPortal(). Called each tick while inside a portal block.
    /// If cooldown > 0: resets it to 10 (prevents it going below 10 while in portal).
    /// If cooldown == 0: sets PortalTrigger = true (initiates dimension travel).
    /// (spec: BlockPortal_Spec §4.2)
    /// </summary>
    public void InPortal()
    {
        if (PortalCooldown > 0)
            PortalCooldown = 10;
        else
            PortalTrigger = true;
    }

    /// <summary>
    /// obf: <c>c(int dim)</c> — travelToDimension. Stub: initiates dimension transfer.
    /// Full implementation requires server-side dimension routing (WorldServer spec pending).
    /// </summary>
    public virtual void TravelToDimension(int dimensionId) { /* stub */ }

    // ── Fishing hook reference ────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>vi.bT</c> — reference to the player's active fishing hook entity, if any.
    /// Set by EntityFishHook on spawn; cleared on reel-in or despawn.
    /// </summary>
    public EntityFishHook? FishHook;

    // ── Arm swing ─────────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>vi.d()</c> — swingArm. Triggers arm-swing animation.
    /// Stub: no-op until renderer reads SwingProgress.
    /// </summary>
    public void SwingArm() { /* stub — renderer reads SwingProgress */ }

    // ── XP ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>vi.a(int amount)</c> — adds experience points.
    /// Advances XpProgress using level formula 7+(cd×7>>1). Levels up while progress ≥ 1.0.
    /// Spec: EnchantingXP_Spec §2.
    /// </summary>
    public void AddXp(int amount)
    {
        Score  += amount;
        XpTotal += amount;
        XpProgress += (float)amount / GetXpToLevel();
        while (XpProgress >= 1.0f)
        {
            XpProgress -= 1.0f;
            XpLevel++;
        }
    }

    /// <summary>
    /// obf: <c>vi.aN()</c> — XP required to advance from current level to next.
    /// Formula: 7 + (XpLevel × 7 >> 1) = 7 + floor(XpLevel × 3.5).
    /// Spec: EnchantingXP_Spec §2.
    /// </summary>
    public int GetXpToLevel() => 7 + (XpLevel * 7 >> 1);

    /// <summary>
    /// obf: <c>vi.l(int value)</c> — deducts <paramref name="levels"/> from XpLevel (minimum 0).
    /// XpProgress and XpTotal are NOT adjusted.
    /// Spec: EnchantingXP_Spec §2.
    /// </summary>
    public void DeductLevels(int levels) => XpLevel = Math.Max(0, XpLevel - levels);

    /// <summary>
    /// obf: <c>vi.b(vi)</c> — XP drop on death: min(XpLevel × 7, 100).
    /// Spec: EnchantingXP_Spec §2.
    /// </summary>
    public int GetXpDrop() => Math.Min(XpLevel * 7, 100);

    // ── Inventory GUI ─────────────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>vi.a(de inventory)</c> — opens a container GUI backed by the given inventory.
    /// Stub until Container_Spec is implemented. Concrete server-player subclass overrides this
    /// to send the open-window packet to the client.
    /// Spec: BlockChest_Spec §6 step 4.
    /// </summary>
    public virtual void OpenInventory(IInventory inventory) { /* stub — Container_Spec pending */ }

    /// <summary>
    /// Opens the 3×3 crafting grid at the given workbench position.
    /// Stub until Container_Spec is implemented.
    /// Spec: BlockWorkbench_Furnace_Cauldron_BrewingStand_Spec §1.4
    /// </summary>
    public virtual void OpenCraftingInventory(int x, int y, int z) { /* stub — Container_Spec pending */ }

    /// <summary>
    /// Opens the sign-editing GUI for the given sign tile entity.
    /// Stub until Container_Spec / GUI_Spec is implemented.
    /// Spec: ItemSign_Spec §3.3
    /// </summary>
    public virtual void OpenSignEditor(TileEntity.TileEntitySign sign) { /* stub — GUI pending */ }
}
