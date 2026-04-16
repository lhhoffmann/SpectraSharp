namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>ih</c> (EntityItem) — the dropped-item entity. Spawned by
/// <c>Entity.DropItem</c> and block-break logic. Bobs in the world and can be
/// picked up by players after a cooldown.
///
/// Size: 0.25 × 0.25 (W × H). Eye height (YOffset) = 0.125.
///
/// Quirks preserved (see spec §9):
///   1. PickupDelay is set to 10 by the drop helper AFTER construction, not inside
///      the constructor. Direct construction leaves PickupDelay = 0.
///   2. Ground friction constant is 0.58800006F (artefact of 0.6F × 0.98F, spec quirk 2).
///   3. Vertical bounce (<c>MotionY *= -0.5F</c>) is applied every tick on ground,
///      not only on landing — causes perpetual micro-oscillation.
///   4. Despawn check is <c>Age >= 6000</c> (not >).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityItem_Spec.md
/// </summary>
public class EntityItem : Entity
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    public  ItemStack ItemStack;     // obf: a — the carried item
    /// <summary>Test alias for ItemStack (compatibility with generated test stubs).</summary>
    public  ItemStack Item => ItemStack;
    private int       _age;          // obf: b — ticks alive; despawn at 6000 (quirk 4)
    public  int       PickupDelay;   // obf: c — must be 0 for pickup to be allowed (quirk 1)
    private float     _rotation;     // obf: d — initial random rotation for visual bob
    private int       _visualTick;   // obf: e — counter driving bob angle in renderer
    private int       _health = 5;   // obf: f — HP; 0 → SetDead

    // ── Constructor (spec §3) ─────────────────────────────────────────────────

    /// <summary>
    /// Spec: <c>ih(ry world, double posX, double posY, double posZ, dk itemStack)</c>.
    /// Spawns with random XZ velocity and upward Y impulse.
    /// </summary>
    public EntityItem(World world, double posX, double posY, double posZ, ItemStack itemStack)
        : base(world)
    {
        // base(world) calls EntityInit() which sets Width=0.25, Height=0.25, YOffset=0.125
        SetPosition(posX, posY, posZ);
        ItemStack    = itemStack;
        _age         = 0;
        PickupDelay  = 0; // caller (drop helper) sets this to 10 after construction (quirk 1)
        _rotation    = EntityRandom.NextFloat() * MathF.PI * 2.0f;
        MotionX      = (EntityRandom.NextFloat() * 2.0f - 1.0f) * 0.1;
        MotionZ      = (EntityRandom.NextFloat() * 2.0f - 1.0f) * 0.1;
        MotionY      = 0.2;
    }

    /// <summary>
    /// NBT-load constructor. Used by <see cref="EntityRegistry.CreateFromNbt"/>.
    /// ItemStack is a placeholder; <see cref="ReadEntityFromNBT"/> replaces it from the tag.
    /// </summary>
    public EntityItem(World world) : base(world)
    {
        ItemStack = new ItemStack(0); // placeholder
    }

    // ── EntityInit (spec §3 step 2–3) ────────────────────────────────────────

    protected override void EntityInit()
    {
        SetSize(0.25f, 0.25f);
        YOffset = Height / 2.0f; // = 0.125F (spec §3 step 3)
    }

    // ── Tick (spec §4) ────────────────────────────────────────────────────────

    /// <summary>
    /// Full per-tick update. Spec: <c>a()</c> override.
    /// Applies gravity, lava bounce, sweep collision, friction, age/despawn, visual counter.
    /// </summary>
    public override void Tick()
    {
        EntityBaseTick(); // fire processing, void kill, prevPos/Rot copy

        if (IsDead) return;

        // Gravity
        MotionY -= 0.04;

        // Lava check: bounce up if standing in lava-material block
        if (World != null)
        {
            int bx = (int)Math.Floor(PosX);
            int by = (int)Math.Floor(PosY);
            int bz = (int)Math.Floor(PosZ);
            Material mat = World.GetBlockMaterial(bx, by, bz);
            if (mat.IsLiquid())
            {
                MotionY = 0.2;
                MotionX = (EntityRandom.NextFloat() * 2.0f - 1.0f) * 0.2;
                MotionZ = (EntityRandom.NextFloat() * 2.0f - 1.0f) * 0.2;
            }
        }

        // Sweep collision
        Move(MotionX, MotionY, MotionZ);

        // Air friction
        MotionX *= 0.98;
        MotionY *= 0.98;
        MotionZ *= 0.98;

        // Ground friction + bounce (quirk 3: applied every tick on ground)
        if (OnGround)
        {
            const float lateralFriction = 0.58800006f; // quirk 2: 0.6F × 0.98F artefact
            MotionX *= lateralFriction;
            MotionZ *= lateralFriction;
            MotionY *= -0.5;            // bounce (quirk 3)
        }

        // Age + despawn (quirk 4: >= not >)
        _age++;
        if (_age >= 6000) SetDead();

        // Pickup check: search for nearby players within radius 1 (spec §5)
        if (!IsDead && PickupDelay == 0 && World != null)
        {
            var nearby = World.GetNearbyPlayers(PosX, PosY, PosZ, 1.0);
            foreach (var player in nearby)
            {
                if (TryPickup(player)) break;
            }
        }

        // Visual counter (drives renderer bobbing)
        _visualTick++;
    }

    // ── Pickup (spec §5) ─────────────────────────────────────────────────────

    /// <summary>
    /// Server-side pickup attempt.
    /// Spec: <c>a(vi player)</c> — adds to player inventory if delay has expired.
    /// </summary>
    public bool TryPickup(EntityPlayer player)
    {
        if (PickupDelay > 0) return false;
        if (ItemStack == null) return false;
        if (player.Inventory.AddItemStackToInventory(ItemStack))
        {
            SetDead(); // item absorbed
            return true;
        }
        return false;
    }

    // ── Damage (spec §6) ─────────────────────────────────────────────────────

    /// <summary>
    /// Deals damage to the item entity (fire, explosions, lava).
    /// Spec: <c>a(DamageSource, int amount)</c>. Health starts at 5.
    /// </summary>
    public void Damage(int amount)
    {
        _health -= amount;
        if (_health <= 0) SetDead();
    }

    // ── Visual accessor ───────────────────────────────────────────────────────

    /// <summary>Ticks alive — drives the despawn counter and can be displayed in debug UIs.</summary>
    public int Age => _age;

    /// <summary>Initial rotation for renderer bobbing. Spec: field <c>d</c>.</summary>
    public float GetInitialRotation() => _rotation;

    /// <summary>Tick counter for renderer bobbing angle. Spec: field <c>e</c>.</summary>
    public int GetVisualTick() => _visualTick;

    // ── NBT hooks (stubs) ─────────────────────────────────────────────────────

    /// <summary>
    /// Writes EntityItem fields. Spec: <c>ih.a(ik tag)</c>.
    /// Quirk: Health is written as <c>(short)((byte)_health)</c> — byte-cast then widen.
    /// No PickupDelay field in Minecraft 1.0 NBT.
    /// </summary>
    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutShort("Health", (short)(byte)_health); // byte-cast quirk (spec §4)
        tag.PutShort("Age",    (short)_age);

        var itemTag = new Nbt.NbtCompound();
        ItemStack.SaveToNbt(itemTag);
        tag.PutCompound("Item", itemTag);
    }

    /// <summary>Reads EntityItem fields. Spec: <c>ih.b(ik tag)</c>.</summary>
    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        _health = tag.GetShort("Health");
        _age    = tag.GetShort("Age");

        Nbt.NbtCompound? itemTag = tag.GetCompound("Item");
        if (itemTag != null)
            ItemStack = ItemStack.LoadFromNbt(itemTag) ?? ItemStack;
    }
}
