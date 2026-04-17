namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>uo</c> (EntityFallingSand) — gravity-driven falling block entity.
///
/// Spawned when a gravity-sensitive block (sand, gravel) loses support. Falls under
/// gravity, then attempts to place the block at the landing position; if placement
/// fails the block is dropped as an item.
///
/// NBT: <c>"Tile"</c> byte — block ID of the carried block (unsigned, 0–255).
///
/// Quirks preserved (see spec §7):
///   7.1 — Bounce without placement when landing on Block ID 36 (movingBlock).
///   7.2 — Gravity applied before physics each tick.
///   7.3 — Drag applied after physics.
///   7.4 — noClip=true: bypasses entity-entity collision.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityFallingSand_Spec.md
/// </summary>
public sealed class EntityFallingSand : Entity
{
    // ── Fields (spec §2) ─────────────────────────────────────────────────────

    /// <summary>obf: <c>a</c> — block ID of the falling block. NBT: "Tile" byte.</summary>
    public int BlockId;         // a

    /// <summary>obf: <c>b</c> — age in ticks; drops as item after 100 ticks.</summary>
    private int _age;           // b

    // ── Block ID that causes bounce-without-placement (spec §4 step 8, quirk 7.1)
    // yy.ac in vanilla. In 1.0 this is effectively never hit (no piston moving block).
    // Using ID 36 (movingBlock) as a safe placeholder.
    private const int BounceBlockId = 36;

    // ── Constructor (spec §4) ─────────────────────────────────────────────────

    public EntityFallingSand(World world, double x, double y, double z, int blockId)
        : base(world)
    {
        BlockId = blockId;
        NoClip  = true;                     // l = true (quirk 7.4)
        SetSize(0.98f, 0.98f);             // a(0.98F, 0.98F) — just under 1 block
        YOffset = Height / 2.0f;           // L = N/2 = 0.49F — eye height at centre
        SetPosition(x, y, z);             // d(x,y,z)
        MotionX = MotionY = MotionZ = 0;   // starts stationary; gravity pulls on tick 1
        PrevPosX = x; PrevPosY = y; PrevPosZ = z;
    }

    // ── EntityInit (called from base constructor — no DataWatcher entries needed) ─

    protected override void EntityInit() { }

    // ── Main tick (spec §4 — Tick a()) ───────────────────────────────────────

    public override void Tick()
    {
        if (World == null) return;

        // Step 1: immediate-remove guard
        if (BlockId == 0) { SetDead(); return; }

        // Step 2: previous-position bookkeeping + age increment
        PrevPosX = PosX; PrevPosY = PosY; PrevPosZ = PosZ;
        _age++;

        // Step 3: apply gravity (spec §4, quirk 7.2 — before physics)
        MotionY -= 0.04f;

        // Step 4: apply physics (sweep-collision)
        Move(MotionX, MotionY, MotionZ);

        // Step 5: apply drag (spec §4, quirk 7.3 — after physics)
        MotionX *= 0.98f;
        MotionY *= 0.98f;
        MotionZ *= 0.98f;

        // Step 6: compute block coordinates
        int bx = (int)Math.Floor(PosX);
        int by = (int)Math.Floor(PosY);
        int bz = (int)Math.Floor(PosZ);

        // Step 7: first-tick block removal (age == 1 after increment)
        if (_age == 1)
        {
            if (World.GetBlockId(bx, by, bz) == BlockId)
            {
                World.SetBlock(bx, by, bz, 0);
            }
            else if (!World.IsClientSide)
            {
                SetDead();
                return;
            }
        }

        // Step 8: landing logic
        if (OnGround)
        {
            // Dampen horizontal velocity (spec §4 step 8)
            MotionX *= 0.7f;
            MotionZ *= 0.7f;
            MotionY *= -0.5; // weak upward bounce (note: double multiply per spec)

            int landId = World.GetBlockId(bx, by, bz);
            if (landId != BounceBlockId)
            {
                // Mark dead and attempt block placement
                SetDead();
                if (!World.IsClientSide)
                {
                    bool canPlace   = !BlockSand.IsFallingBelow(World, bx, by - 1, bz);
                    bool setSuccess = canPlace && World.SetBlock(bx, by, bz, BlockId);
                    if (!setSuccess)
                    {
                        // Drop as item
                        DropBlock();
                    }
                }
            }
        }

        // Step 9: age-out timeout
        if (_age > 100 && !World.IsClientSide && !OnGround)
        {
            DropBlock();
            SetDead();
        }
    }

    // ── Drop the block as an item entity ─────────────────────────────────────

    private void DropBlock()
    {
        if (World is not World concreteWorld) return;
        var drop = new EntityItem(concreteWorld, PosX, PosY, PosZ, new ItemStack(BlockId, 1, 0));
        drop.PickupDelay = 10;
        concreteWorld.SpawnEntity(drop);
    }

    // ── NBT (spec §4 NBT section) ─────────────────────────────────────────────

    protected override void WriteEntityToNBT(Nbt.NbtCompound tag)
    {
        tag.PutByte("Tile", (byte)BlockId);
    }

    protected override void ReadEntityFromNBT(Nbt.NbtCompound tag)
    {
        BlockId = tag.GetByte("Tile") & 255;
    }
}
