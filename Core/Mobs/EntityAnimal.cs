namespace SpectraEngine.Core.Mobs;

/// <summary>
/// Replica of <c>fx</c> (EntityAnimal) — abstract base for all breedable animals.
/// Extends <see cref="EntityAI"/>; adds age/love timer (DataWatcher 12) and
/// the full breed/follow/baby AI sequence.
///
/// Age encoding (DataWatcher slot 12):
///   0        = adult
///   negative = baby (starts −24000, counts toward 0 over ~20 min)
///   positive = breeding cooldown (set to 6000 after breeding, counts toward 0)
///
/// AI modes via o() (spec §10):
///   1. InLove: find breed partner (same species, also inLove).
///   2. Adult with food nearby: follow player holding food.
///   3. Adult on cooldown: follow babies of own species.
///
/// NBT: Age (int), InLove (int).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MobAI_PathFinder_Spec.md §10
/// </summary>
public abstract class EntityAnimal : EntityAI
{
    // ── Fields (spec §10) ────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — inLove timer (set to 600 on feed; counts to 0).</summary>
    protected int InLoveTimer;      // obf: a

    /// <summary>obf: <c>b</c> — breeding proximity counter; reaches 60 to trigger offspring.</summary>
    private int _breedCounter;      // obf: b

    // ── Constructor ──────────────────────────────────────────────────────────

    protected EntityAnimal(World world) : base(world) { }

    // ── EntityInit ───────────────────────────────────────────────────────────

    protected override void EntityInit()
    {
        base.EntityInit();
        DataWatcher.Register(12, 0); // age int
    }

    // ── Age accessors (spec §3 m()/b(int)) ───────────────────────────────────

    /// <summary>obf: <c>m()</c> — reads age from DataWatcher.</summary>
    public int GetAge() => DataWatcher.GetInt(12);

    /// <summary>obf: <c>b(int)</c> — writes age to DataWatcher.</summary>
    public void SetAge(int age) => DataWatcher.UpdateObject(12, age);

    /// <summary>Returns true if this animal is a baby (age &lt; 0).</summary>
    public bool IsBaby() => GetAge() < 0;

    // ── Food check (spec §10 a(dk)) — can be overridden per species ──────────

    /// <summary>
    /// obf: <c>fx.a(dk item)</c> — returns true if the item is "food" for this animal.
    /// Default: wheat (ID 296). Override per species (e.g. carrot for pigs).
    /// </summary>
    protected virtual bool IsFood(ItemStack item) => item.ItemId == 296; // wheat

    // ── Spawn check (spec §10 i()) ────────────────────────────────────────────

    public override bool GetCanSpawnHere()
    {
        if (World == null) return false;
        int bx = (int)Math.Floor(PosX);
        int by = (int)Math.Floor(BoundingBox.MinY);
        int bz = (int)Math.Floor(PosZ);
        return World.GetBlockId(bx, by - 1, bz) == 2 // grass block ID 2
            && World.GetLightBrightness(bx, by, bz) > 8
            && base.GetCanSpawnHere();
    }

    // ── Position score (spec §10 a(x,y,z)) — prefer grass / light ────────────

    protected override float GetPositionScore(int x, int y, int z)
    {
        if (World == null) return 0f;
        if (World.GetBlockId(x, y - 1, z) == 2) return 10.0f; // grass: strong preference
        return World.GetBrightness(x, y, z, 0) - 0.5f;         // otherwise prefer light
    }

    // ── Target acquisition (spec §10 o()) ────────────────────────────────────

    protected override Entity? GetAITarget()
    {
        if (World == null) return null;
        if (PanicTimer > 0) return null; // panicking: no target

        int age = GetAge();

        if (InLoveTimer > 0)
        {
            // Mode 1: find same-species breed partner also in love
            var aabb = BoundingBox.Expand(8, 8, 8);
            foreach (EntityAnimal candidate in World.GetEntitiesWithinAABB<EntityAnimal>(aabb))
            {
                if (candidate == this) continue;
                if (candidate.GetType() == GetType() && candidate.InLoveTimer > 0)
                    return candidate;
            }
        }
        else if (age == 0)
        {
            // Mode 2: adult – follow player holding food
            var aabb = BoundingBox.Expand(8, 8, 8);
            foreach (EntityPlayer player in World.GetEntitiesWithinAABB<EntityPlayer>(aabb))
            {
                ItemStack? held = player.GetEquippedItem();
                if (held != null && IsFood(held))
                    return player;
            }
        }
        else if (age > 0)
        {
            // Mode 3: breeding cooldown – approach own-species babies
            var aabb = BoundingBox.Expand(8, 8, 8);
            foreach (EntityAnimal candidate in World.GetEntitiesWithinAABB<EntityAnimal>(aabb))
            {
                if (candidate == this) continue;
                if (candidate.GetType() == GetType() && candidate.IsBaby())
                    return candidate;
            }
        }

        return null;
    }

    // ── Attack range check ────────────────────────────────────────────────────

    protected override bool IsInRange(Entity target)
    {
        double dx = target.PosX - PosX;
        double dy = target.PosY - PosY;
        double dz = target.PosZ - PosZ;
        double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (target is EntityAnimal partnerAnimal)
        {
            if (IsBaby())
                return dist < 2.5;           // mode 3: follow babies
            return dist < 3.5;               // mode 1: breed partner
        }
        return dist < 3.0; // mode 2: player
    }

    // ── Approach / breed behavior (spec §10 a(target, dist)) ─────────────────

    protected override void OnTargetInRange(Entity target, float dist)
    {
        if (target is EntityPlayer player)
        {
            // Mode 2: close to food-carrying player
            RotationYaw = FaceToward(player);
            IsAngry = true; // force facing player during path follow

            // If player drops the food, lose interest
            ItemStack? held = player.GetEquippedItem();
            if (held == null || !IsFood(held))
                AiTarget = null;
        }
        else if (target is EntityAnimal partner)
        {
            int age = GetAge();

            if (InLoveTimer > 0 && partner.GetAge() < 0)
            {
                // Mode 3: close to a baby — just face it
                if (dist < 2.5f) IsAngry = true;
            }
            else if (InLoveTimer > 0 && partner.InLoveTimer > 0)
            {
                // Mode 1: breed sequence
                if (partner.AiTarget == null)
                    partner.AiTarget = this;

                if (partner.AiTarget == this)
                {
                    _breedCounter++;
                    partner._breedCounter++;

                    if (_breedCounter % 4 == 0)
                    {
                        // Heart particles (stub — particle system not yet implemented)
                    }

                    if (_breedCounter >= 60)
                        Breed(partner);
                }
                else
                {
                    _breedCounter = 0;
                }
            }
            else
            {
                _breedCounter = 0;
            }
        }
    }

    protected override void OnTargetOutOfRange(Entity target, float dist)
    {
        if (target is EntityAnimal)
            _breedCounter = 0;
    }

    // ── Breed and spawn offspring (spec §10 b(fx)) ───────────────────────────

    private void Breed(EntityAnimal partner)
    {
        if (World == null) return;

        EntityAnimal? offspring = CreateOffspring(partner);
        if (offspring == null) return;

        // Reset both parents
        InLoveTimer = 0; _breedCounter = 0; AiTarget = null;
        partner.AiTarget = null; partner._breedCounter = 0; partner.InLoveTimer = 0;

        // Apply breeding cooldown (5 minutes = 6000 ticks)
        SetAge(6000);
        partner.SetAge(6000);

        // Offspring is a baby for 20 minutes (−24000 ticks)
        offspring.SetAge(-24000);
        offspring.SetPosition(PosX, PosY, PosZ);

        // Spawn 7 heart particles (stub)

        World.SpawnEntity(offspring);
    }

    /// <summary>
    /// obf: <c>a(fx partner)</c> — abstract. Creates the species-specific offspring.
    /// Return null to suppress breeding (unusual but possible).
    /// </summary>
    protected abstract EntityAnimal? CreateOffspring(EntityAnimal partner);

    // ── Player right-click: feed to enter love mode (spec §10 c(vi)) ─────────

    /// <summary>
    /// obf: <c>fx.c(vi player)</c> — right-click feeding.
    /// Consumes one food item, enters love mode for 600 ticks.
    /// </summary>
    public virtual bool Interact(EntityPlayer player)
    {
        ItemStack? held = player.GetEquippedItem();
        if (held != null && IsFood(held) && GetAge() == 0)
        {
            held.StackSize--;
            // Clear empty stack from inventory
            if (held.StackSize <= 0)
                player.Inventory.SetInventorySlotContents(player.Inventory.CurrentItem, null);

            InLoveTimer = 600;
            AiTarget    = null;
            // Spawn heart particles (stub)
            return true;
        }
        return false;
    }

    // ── Tick (spec §10 fx.c()) ────────────────────────────────────────────────

    public override void Tick()
    {
        base.Tick(); // EntityAI.Tick → RunAITick

        if (World == null || World.IsClientSide) return;

        // Age progression
        int age = GetAge();
        if (age < 0) SetAge(age + 1); // baby growing up
        if (age > 0) SetAge(age - 1); // cooldown counting down

        // Love timer + breeding counter reset
        if (InLoveTimer > 0)
        {
            InLoveTimer--;
            if (InLoveTimer % 10 == 0)
            {
                // Spawn heart particle (stub)
            }
        }
        else
        {
            _breedCounter = 0;
        }
    }

    // ── On-damage: panic + clear love (spec §10 fx.a(DamageSource,int)) ──────

    public override bool AttackEntityFrom(DamageSource damageSource, int amount)
    {
        PanicTimer  = 60;    // panic sprint for 60 ticks
        AiTarget    = null;  // clear target
        InLoveTimer = 0;     // cancel love mode
        return base.AttackEntityFrom(damageSource, amount);
    }

    // ── XP on death (spec §10 b(vi)) ─────────────────────────────────────────

    public override int GetMaxHealth() => 10;

    // ── NBT (spec §10) ────────────────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        base.WriteEntityToNBT(tag);
        tag.PutInt("Age",    GetAge());
        tag.PutInt("InLove", InLoveTimer);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        base.ReadEntityFromNBT(tag);
        SetAge(tag.GetInt("Age"));
        InLoveTimer = tag.GetInt("InLove");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private float FaceToward(Entity target)
    {
        double dx = target.PosX - PosX;
        double dz = target.PosZ - PosZ;
        return (float)(Math.Atan2(dz, dx) * (180.0 / Math.PI)) - 90.0f;
    }
}
