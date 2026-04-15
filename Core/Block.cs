using System.Collections.Generic;

namespace SpectraSharp.Core;

/// <summary>
/// Bit-exact replica of <c>yy</c> (Block) — base class for all block types.
///
/// Three roles:
///   1. Static registry  — Block?[256] indexed by ID + eight parallel metadata arrays.
///   2. Block singletons — all vanilla blocks are public static fields of this class.
///   3. Virtual behaviour contract — default implementations for every block event.
///
/// Quirks preserved (see spec §12):
///   1. setHardness raises resistance to hardness*5 as a minimum; setResistance overwrites.
///   2. collisionRayTrace uses Euclidean distance (DistanceTo) not squared — unlike AABB.rayTrace.
///   3. collisionRayTrace translates ray into block-local space before intersecting.
///   4. dropBlockAsItemWithChance jitter: (rnd*0.7)+0.15 per axis.
///   5. Spawned EntityItem has 10-tick pickup delay.
///   6. shouldSideBeRendered face IDs: 0=bottom,1=top,2=north,3=south,4=west,5=east.
///   7. isOpaqueCube is called virtually in the constructor via this.IsOpaqueCube().
///
/// Type correction (wu/p swap — see StepSound_Spec.md / Material_Spec.md):
///   wu = StepSound, p = Material. Block_Spec annotations were inverted.
///   bX = StepSound, bZ = Material. Constructor takes Material.
///   p[] array = CanPassThrough (false for solid blocks, true for air slot 0).
///
/// Open dependencies (see REQUESTS.md):
///   IWorld (ry), Player (vi), ItemStack (dk), EntityItem (ih), ny initialiser.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/Block_Spec.md
/// </summary>
public class Block
{
    // ── Static step-sound constants (spec §2, obf: b–j; defined on Block, not on StepSound) ──

    /// <summary>obf: b — stone step sound. Default sound for most blocks.</summary>
    public static readonly StepSound SoundStone         = new StepSound("stone",  1.0f, 1.0f);  // b
    /// <summary>obf: c — wood step sound.</summary>
    public static readonly StepSound SoundWood          = new StepSound("wood",   1.0f, 1.0f);  // c
    /// <summary>obf: d — gravel step sound. Used for dirt, sand, gravel.</summary>
    public static readonly StepSound SoundGravel        = new StepSound("gravel", 1.0f, 1.0f);  // d
    /// <summary>obf: e — grass step sound.</summary>
    public static readonly StepSound SoundGrass         = new StepSound("grass",  1.0f, 1.0f);  // e
    /// <summary>obf: f — stone step sound with higher pitch (1.5×).</summary>
    public static readonly StepSound SoundStoneHighPitch  = new StepSound("stone", 1.0f, 1.5f); // f
    /// <summary>obf: g — stone step sound with higher pitch (1.5×). Identical to f.</summary>
    public static readonly StepSound SoundStoneHighPitch2 = new StepSound("stone", 1.0f, 1.5f); // g
    /// <summary>obf: h — glass/liquid break sound (<c>bj</c> subclass). GetPlaceSound → "random.glass".</summary>
    public static readonly StepSound SoundGlass         = new StepSound.GlassStepSound("stone", 1.0f, 1.0f); // h
    /// <summary>obf: i — cloth/wool step sound.</summary>
    public static readonly StepSound SoundCloth         = new StepSound("cloth",  1.0f, 1.0f);  // i
    /// <summary>obf: j — sand step sound (<c>aeg</c> subclass). GetPlaceSound → "step.gravel".</summary>
    public static readonly StepSound SoundSand          = new StepSound.SandStepSound("sand", 1.0f, 1.0f);  // j

    // ── Static registry arrays (spec §3, size 256) ───────────────────────────

    public  static readonly Block?[]    BlocksList       = new Block?[256];    // obf: k
    public  static readonly bool[]      IsBlockContainer = new bool[256];      // obf: l
    public  static readonly bool[]      IsOpaqueCubeArr  = new bool[256];      // obf: m
    private static readonly bool[]      _unknownN        = new bool[256];      // obf: n  always false
    public  static readonly int[]       LightOpacity     = new int[256];       // obf: o  0–255
    public  static readonly bool[]      CanPassThrough   = new bool[256];      // obf: p  true = passable (air/liquid); false = solid
    public  static readonly int[]       LightValue       = new int[256];       // obf: q  0–15
    public  static readonly bool[]      HasTileEntity    = new bool[256];      // obf: r
    public  static readonly bool[]      RenderSpecial    = new bool[256];      // obf: s
    /// <summary>
    /// obf: static <c>ca[256]</c> — per-block slipperiness lookup table.
    /// Default 0.6F. Ice = 0.98F. Initialised from Block instance field when block registers.
    /// Used by LivingEntity friction formula: <c>Block.SlipperinessMap[id] * 0.91F</c>.
    /// </summary>
    public  static readonly float[]    SlipperinessMap  = Enumerable.Repeat(0.6f, 256).ToArray(); // obf: ca[]

    // ── Instance fields (spec §4) ─────────────────────────────────────────────

    public  int       BlockIndexInTexture;               // obf: bL
    public  readonly int BlockID;                        // obf: bM  (final in Java)
    public  float     BlockHardness;                     // obf: bN
    public  float     BlockResistance;                   // obf: bO
#pragma warning disable CS0414  // spec-required fields — purpose TBD, must be preserved
    private bool      _bP = true;                        // obf: bP  always true, purpose TBD
    public  bool      NeedsRandomTick = true;            // obf: bQ  false after builder r()
    public  double    MinX;                              // obf: bR
    public  double    MinY;                              // obf: bS
    public  double    MinZ;                              // obf: bT
    public  double    MaxX = 1.0;                        // obf: bU
    public  double    MaxY = 1.0;                        // obf: bV
    public  double    MaxZ = 1.0;                        // obf: bW
    public  StepSound? StepSoundGroup;                   // obf: bX  default = SoundStone; set via SetStepSound()
    private readonly float _bY = 1.0f;                  // obf: bY  purpose TBD
#pragma warning restore CS0414
    public  Material? BlockMaterial;                     // obf: bZ  (final in Java) set in constructor
    /// <summary>
    /// obf: <c>ca</c> (instance) — per-block slipperiness. Default 0.6F; ice = 0.98F.
    /// Written into <see cref="SlipperinessMap"/> when block registers.
    /// </summary>
    public float Slipperiness = 0.6f;                   // obf: ca (instance)
    public  string?   BlockName;                         // obf: a   "tile.xxx"

    // ── Constructors (spec §5) ────────────────────────────────────────────────

    /// <summary>
    /// 2-argument constructor. Spec: <c>yy(int var1, p var2)</c>.
    /// Registers the block in <see cref="BlocksList"/> and initialises metadata arrays.
    /// Throws if the slot is already occupied (quirk 7 — virtual isOpaqueCube call).
    /// </summary>
    public Block(int blockId, Material material)
    {
        if (BlocksList[blockId] is not null)
            throw new InvalidOperationException(
                $"Slot {blockId} is already occupied by {BlocksList[blockId]} when adding {this}");

        BlockMaterial    = material;
        BlocksList[blockId] = this;
        BlockID          = blockId;

        SetBounds(0f, 0f, 0f, 1f, 1f, 1f);

        // Virtual call — subclass override resolves at construction time (quirk 7)
        IsOpaqueCubeArr[blockId]  = IsOpaqueCube();
        LightOpacity[blockId]     = IsOpaqueCube() ? 255 : 0;
        CanPassThrough[blockId]   = !material.BlocksMovement();
        _unknownN[blockId]        = false;
        SlipperinessMap[blockId]  = Slipperiness; // default 0.6F; set before subclass can change it
        RenderSpecial[blockId]    = true;          // s[id] = true by default; cleared via ClearNeedsRandomTick()
    }

    /// <summary>
    /// 3-argument constructor. Spec: <c>yy(int var1, int var2, p var3)</c>.
    /// Delegates to 2-arg, then sets texture index.
    /// </summary>
    public Block(int blockId, int textureIndex, Material material)
        : this(blockId, material)
    {
        BlockIndexInTexture = textureIndex;
    }

    // ── Builder methods (spec §6, all return this) ────────────────────────────

    /// <summary>
    /// Sets hardness and raises blast resistance to at least hardness*5 (quirk 1).
    /// Spec: <c>c(float)</c>.
    /// </summary>
    public Block SetHardness(float hardness)
    {
        BlockHardness = hardness;
        if (BlockResistance < hardness * 5.0f)
            BlockResistance = hardness * 5.0f;
        return this;
    }

    /// <summary>Sets blast resistance. Overwrites any minimum set by SetHardness (quirk 1). Spec: <c>b(float)</c>.</summary>
    public Block SetResistance(float resistance)
    {
        BlockResistance = resistance * 3.0f;
        return this;
    }

    /// <summary>Makes the block unbreakable (hardness = -1). Spec: <c>m()</c>.</summary>
    public Block SetUnbreakable()
    {
        SetHardness(-1.0f);
        return this;
    }

    /// <summary>Sets emitted light level from a [0,1] fraction. Spec: <c>a(float)</c>.</summary>
    public Block SetLightValue(float fraction)
    {
        LightValue[BlockID] = (int)(15.0f * fraction);
        return this;
    }

    /// <summary>Overrides light opacity. Spec: <c>h(int)</c>.</summary>
    public Block SetLightOpacity(int opacity)
    {
        LightOpacity[BlockID] = opacity;
        return this;
    }

    /// <summary>Sets the step-sound group. Spec: <c>a(wu)</c> — wu = StepSound.</summary>
    public Block SetStepSound(StepSound stepSound)
    {
        StepSoundGroup = stepSound;
        return this;
    }

    /// <summary>Marks this block as having a tile entity. Spec: <c>l()</c>.</summary>
    public Block SetHasTileEntity()
    {
        HasTileEntity[BlockID] = true;
        return this;
    }

    /// <summary>Clears random tick requirement. Spec: <c>r()</c>.</summary>
    public Block ClearNeedsRandomTick()
    {
        NeedsRandomTick = false;
        RenderSpecial[BlockID] = false;
        return this;
    }

    /// <summary>Sets container flag. Spec: <c>b(boolean)</c>.</summary>
    public Block SetIsContainer(bool value)
    {
        IsBlockContainer[BlockID] = value;
        return this;
    }

    /// <summary>Sets translation key to "tile." + name. Spec: <c>a(String)</c>.</summary>
    public Block SetBlockName(string name)
    {
        BlockName = "tile." + name;
        return this;
    }

    // ── Virtual behaviour contract (spec §7) ─────────────────────────────────

    /// <summary>True if this block is a full opaque cube. Cached in <see cref="IsOpaqueCubeArr"/> at construction. Spec: <c>a()</c>.</summary>
    public virtual bool IsOpaqueCube() => true;

    /// <summary>True if this block renders as a normal cube. Spec: <c>k()</c>.</summary>
    public virtual bool RenderAsNormalBlock() => true;

    /// <summary>True if entities can collide with this block. Spec: <c>b()</c>.</summary>
    public virtual bool IsCollidable() => true;

    /// <summary>Used in static initializer to flag special-rendering blocks. Default 0. Spec: <c>c()</c>.</summary>
    public virtual int GetTickRandomly() => 0;

    /// <summary>Returns raw hardness value. Spec: <c>n()</c>.</summary>
    public float GetHardness() => BlockHardness;

    /// <summary>Returns mobility flag for piston behaviour. 0=pushable, 1=immovable, 2=push-only. Spec: <c>h()</c>.</summary>
    public virtual int GetMobilityFlag() => 0;

    /// <summary>True if the block can stay in place. Spec: <c>e(World,x,y,z)</c> default.</summary>
    public virtual bool CanBlockStay(IWorld world, int x, int y, int z) => true;

    /// <summary>Number of items dropped on break. Default 1. Spec: <c>a(Random)</c>.</summary>
    public virtual int QuantityDropped(JavaRandom rng) => 1;

    /// <summary>Item ID dropped. Default = blockID. Spec: <c>a(int meta, Random, int fortune)</c>.</summary>
    public virtual int IdDropped(int metadata, JavaRandom rng, int fortune) => BlockID;

    /// <summary>Quantity dropped with fortune. Default ignores fortune. Spec: <c>a(int fortune, Random)</c>.</summary>
    public virtual int QuantityDroppedWithBonus(int fortune, JavaRandom rng)
        => QuantityDropped(rng);

    /// <summary>
    /// Metadata (damage value) of the dropped item. Default 0.
    /// Override to preserve the block's own metadata in the drop (e.g. wool colour, slab variant).
    /// Spec: <c>a(int meta) — getDamageValue</c>.
    /// </summary>
    public virtual int DamageDropped(int meta) => 0;

    /// <summary>Returns tick delay denominator. Default 10. Spec: <c>d()</c>.</summary>
    public virtual int GetTickDelay() => 10;

    /// <summary>True if this block has a tile entity (virtual override route). Spec: <c>g()</c>.</summary>
    public virtual bool HasTileEntityVirtual() => false;

    /// <summary>
    /// True if the block at (x,y,z) has a solid material.
    /// Reads material from world, calls IsSolid(). Spec: <c>e(kq,x,y,z,face)</c>.
    /// </summary>
    public virtual bool IsNormalCube(IBlockAccess world, int x, int y, int z, int face)
        => world.GetBlockMaterial(x, y, z).IsSolid();

    /// <summary>Default: returns false. Spec: <c>b(IBlockAccess,x,y,z,face)</c>.</summary>
    public virtual bool IsSideSolid(IBlockAccess world, int x, int y, int z, int face) => false;

    /// <summary>Default: returns false. Spec: <c>c(World,x,y,z,face)</c>.</summary>
    public virtual bool CanProvideSupport(IWorld world, int x, int y, int z, int face) => false;

    // ── Redstone power API (spec: BlockRedstone_Spec §2) ─────────────────────

    /// <summary>
    /// obf: <c>b(kq,x,y,z,face)</c> — isProvidingWeakPower.
    /// Returns true if this block provides weak redstone power toward <paramref name="face"/>.
    /// Default: false. Overridden by redstone components.
    /// </summary>
    public virtual bool IsProvidingWeakPower(IBlockAccess world, int x, int y, int z, int face) => false;

    /// <summary>
    /// obf: <c>c(ry,x,y,z,face)</c> — isProvidingStrongPower.
    /// Returns true if this block provides strong redstone power toward <paramref name="face"/>.
    /// Default: false. Overridden by torches, levers, buttons, pressure plates.
    /// </summary>
    public virtual bool IsProvidingStrongPower(IWorld world, int x, int y, int z, int face) => false;

    /// <summary>
    /// obf: <c>g()</c> — canProvidePower.
    /// True for all blocks that actively emit redstone power.
    /// Default: false. Wire and powered blocks return true.
    /// </summary>
    public virtual bool CanProvidePower() => false;

    /// <summary>Returns the texture index for a given face. Default: ignores face. Spec: <c>b(int face)</c>.</summary>
    public virtual int GetTextureIndex(int face) => BlockIndexInTexture;

    /// <summary>Returns the texture for a face+metadata combination. Default: delegates to GetTextureIndex. Spec: <c>a(int face, int meta)</c>.</summary>
    public virtual int GetTextureForFaceAndMeta(int face, int meta) => GetTextureIndex(face);

    /// <summary>Returns tint colour multiplier. Default white (0xFFFFFF). Spec: <c>f()</c>.</summary>
    public virtual int GetRenderColor() => 16777215;

    /// <summary>Returns tint for given metadata. Default white. Spec: <c>c(int meta)</c>.</summary>
    public virtual int GetColorFromMetadata(int meta) => 16777215;

    /// <summary>Returns world-position tint. Default white. Spec: <c>a(IBlockAccess,x,y,z)</c>.</summary>
    public virtual int GetMixedBrightnessForBlock(IBlockAccess world, int x, int y, int z)
        => 16777215;

    /// <summary>Random tick. Default no-op. Spec: <c>a(World,x,y,z,Random)</c>.</summary>
    public virtual void BlockTick(IWorld world, int x, int y, int z, JavaRandom rng) { }

    /// <summary>Scheduled tick. Default no-op. Spec: <c>b(World,x,y,z,Random)</c>.</summary>
    public virtual void UpdateTick(IWorld world, int x, int y, int z, JavaRandom rng) { }

    /// <summary>Called when a neighbour changes. Default no-op. Spec: <c>e(World,x,y,z,int neighbourId)</c>.</summary>
    public virtual void OnNeighborBlockChange(IWorld world, int x, int y, int z, int neighbourId) { }

    /// <summary>
    /// Called when a player right-clicks the block. Returns true to consume the click.
    /// Spec: <c>a(ry, x, y, z, vi player)</c>.
    /// </summary>
    public virtual bool OnBlockActivated(IWorld world, int x, int y, int z, EntityPlayer player) => false;

    /// <summary>
    /// Called each tick while an entity walks on top of this block.
    /// Spec: <c>b(ry, x, y, z, ia entity)</c>.
    /// </summary>
    public virtual void OnEntityWalking(IWorld world, int x, int y, int z, Entity entity) { }

    /// <summary>
    /// Called each tick while an entity's bounding box overlaps this block.
    /// Used by portals (BlockPortal ID 90, BlockEndPortal ID 119) and other contact triggers.
    /// Spec: <c>a(ry, x, y, z, ia)</c> — onEntityCollidedWithBlock.
    /// </summary>
    public virtual void OnEntityCollidedWithBlock(IWorld world, int x, int y, int z, Entity entity) { }

    /// <summary>Called when placed. Default no-op. Spec: <c>a(World,x,y,z)</c>.</summary>
    public virtual void OnBlockAdded(IWorld world, int x, int y, int z) { }

    /// <summary>Called when removed. Default no-op. Spec: <c>d(World,x,y,z)</c>.</summary>
    public virtual void OnBlockRemoved(IWorld world, int x, int y, int z) { }

    /// <summary>
    /// Called immediately before a block is replaced in the world, while the tile entity
    /// still exists. Container blocks (e.g. jukebox) use this to eject stored items.
    /// Spec: <c>abl.d(ry,x,y,z)</c> — onBlockPreDestroy. Default no-op.
    /// </summary>
    public virtual void OnBlockPreDestroy(IWorld world, int x, int y, int z) { }

    /// <summary>Called when destroyed by player. Default no-op. Spec: <c>a(World,x,y,z,int meta)</c>.</summary>
    public virtual void OnBlockDestroyedByPlayer(IWorld world, int x, int y, int z, int meta) { }

    /// <summary>
    /// Updates shape bounds to reflect current state. Default no-op.
    /// Called at the start of <see cref="CollisionRayTrace"/> (quirk 3).
    /// Spec: <c>b(IBlockAccess,x,y,z)</c>.
    /// </summary>
    public virtual void SetBlockBoundsBasedOnState(IBlockAccess world, int x, int y, int z) { }

    // ── Bounds ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets block-local bounding box. Values are cast from float to double.
    /// Default full unit cube: (0,0,0) → (1,1,1). Spec: <c>a(float×6)</c>.
    /// </summary>
    public void SetBounds(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        MinX = (double)minX; MinY = (double)minY; MinZ = (double)minZ;
        MaxX = (double)maxX; MaxY = (double)maxY; MaxZ = (double)maxZ;
    }

    /// <summary>
    /// Returns the world-space collision AABB (pooled) for this block at (x, y, z).
    /// Spec: <c>c_(World,x,y,z)</c>.
    /// </summary>
    public virtual AxisAlignedBB? GetCollisionBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(
            x + MinX, y + MinY, z + MinZ,
            x + MaxX, y + MaxY, z + MaxZ);

    /// <summary>
    /// Returns the world-space selection highlight AABB (pooled). Default = collision box.
    /// Spec: <c>b(World,x,y,z)</c>.
    /// </summary>
    public virtual AxisAlignedBB GetSelectedBoundingBoxFromPool(IWorld world, int x, int y, int z)
        => AxisAlignedBB.GetFromPool(
            x + MinX, y + MinY, z + MinZ,
            x + MaxX, y + MaxY, z + MaxZ);

    /// <summary>
    /// Adds the block's collision box to <paramref name="list"/> if it intersects
    /// <paramref name="entityBox"/>. Spec: <c>a(World,x,y,z,AABB entityBox,List)</c>.
    /// </summary>
    public virtual void AddCollisionBoxesToList(
        IWorld world, int x, int y, int z,
        AxisAlignedBB entityBox, List<AxisAlignedBB> list)
    {
        AxisAlignedBB box = GetSelectedBoundingBoxFromPool(world, x, y, z);
        if (entityBox.Intersects(box))
            list.Add(box);
    }

    // ── Ray trace (spec §7) ───────────────────────────────────────────────────

    /// <summary>
    /// World-space ray intersection with this block's bounding box.
    /// Returns null if no face is hit.
    ///
    /// Quirks:
    ///   - Uses <c>Vec3.DistanceTo</c> (Euclidean, float-precision) NOT squared distance
    ///     (quirk 2 — different from AxisAlignedBB.rayTrace which uses squared).
    ///   - Ray is translated into block-local space before intersecting, then back (quirk 3).
    ///
    /// Spec: <c>a(World,x,y,z,Vec3 start,Vec3 end)</c>.
    /// </summary>
    public virtual MovingObjectPosition? CollisionRayTrace(
        IWorld world, int x, int y, int z, Vec3 start, Vec3 end)
    {
        // Step 1 — refresh shape (no-op in base class)
        SetBlockBoundsBasedOnState(world, x, y, z);

        // Step 2 — translate ray into block-local space
        Vec3 localStart = start.Add(-(double)x, -(double)y, -(double)z);
        Vec3 localEnd   = end.Add(  -(double)x, -(double)y, -(double)z);

        // Step 3 — six face-plane intersections (block-local bounds)
        Vec3? v7  = localStart.GetIntermediateWithXValue(localEnd, MinX);
        Vec3? v8  = localStart.GetIntermediateWithXValue(localEnd, MaxX);
        Vec3? v9  = localStart.GetIntermediateWithYValue(localEnd, MinY);
        Vec3? v10 = localStart.GetIntermediateWithYValue(localEnd, MaxY);
        Vec3? v11 = localStart.GetIntermediateWithZValue(localEnd, MinZ);
        Vec3? v12 = localStart.GetIntermediateWithZValue(localEnd, MaxZ);

        // Step 4 — validate with closed-interval bounds checks
        if (!IsOnYzFace(v7))  v7  = null;
        if (!IsOnYzFace(v8))  v8  = null;
        if (!IsOnXzFace(v9))  v9  = null;
        if (!IsOnXzFace(v10)) v10 = null;
        if (!IsOnXyFace(v11)) v11 = null;
        if (!IsOnXyFace(v12)) v12 = null;

        // Step 5 — closest by EUCLIDEAN distance (quirk 2 — NOT squared distance)
        Vec3? best = null;
        double bestDist = double.MaxValue;
        foreach (Vec3? c in (ReadOnlySpan<Vec3?>)[v7, v8, v9, v10, v11, v12])
        {
            if (c is null) continue;
            double dist = localStart.DistanceTo(c);
            if (dist < bestDist) { best = c; bestDist = dist; }
        }

        // Step 6 — no hit
        if (best is null) return null;

        // Step 7 — face ID (sequential, last match wins)
        int faceId = -1;
        if (best == v7)  faceId = 4;
        if (best == v8)  faceId = 5;
        if (best == v9)  faceId = 0;
        if (best == v10) faceId = 1;
        if (best == v11) faceId = 2;
        if (best == v12) faceId = 3;

        // Step 8 — translate hit point back to world space
        Vec3 worldHit = best.Add((double)x, (double)y, (double)z);

        // Step 9 — return with real block coordinates
        return new MovingObjectPosition(x, y, z, faceId, worldHit);
    }

    // Private ray-face validators (closed intervals — same as AxisAlignedBB spec §6)
    private bool IsOnYzFace(Vec3? v)
        => v is not null && v.Y >= MinY && v.Y <= MaxY && v.Z >= MinZ && v.Z <= MaxZ;
    private bool IsOnXzFace(Vec3? v)
        => v is not null && v.X >= MinX && v.X <= MaxX && v.Z >= MinZ && v.Z <= MaxZ;
    private bool IsOnXyFace(Vec3? v)
        => v is not null && v.X >= MinX && v.X <= MaxX && v.Y >= MinY && v.Y <= MaxY;

    // ── Rendering queries (spec §7) ───────────────────────────────────────────

    /// <summary>
    /// True if face <paramref name="face"/> should be rendered (not fully covered).
    /// Uses face IDs 0–5: 0=bottom,1=top,2=north,3=south,4=west,5=east (quirk 6).
    /// Default case delegates to <c>!world.IsBlockOpaque(neighbour)</c>.
    /// Spec: <c>a_(IBlockAccess,x,y,z,face)</c>.
    /// </summary>
    public virtual bool ShouldSideBeRendered(IBlockAccess world, int x, int y, int z, int face)
    {
        return face switch
        {
            0 => MinY > 0.0,
            1 => MaxY < 1.0,
            2 => MinZ > 0.0,
            3 => MaxZ < 1.0,
            4 => MinX > 0.0,
            5 => MaxX < 1.0,
            _ => !world.IsOpaqueCube(x, y, z)
        };
    }

    /// <summary>
    /// Returns ambient occlusion brightness. Spec: <c>d(float,IBlockAccess,x,y,z)</c>.
    /// </summary>
    public float GetLightBrightness(float unused, IBlockAccess world, int x, int y, int z)
        => world.GetBrightness(x, y, z, LightValue[BlockID]);

    /// <summary>
    /// True if the block at (x,y,z) can be replaced by another block.
    /// Air (ID 0) or replaceable material. Spec: <c>c(World,x,y,z)</c>.
    /// </summary>
    public virtual bool CanReplace(IWorld world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        if (id == 0) return true;
        Block? b = BlocksList[id];
        return b?.BlockMaterial?.IsReplaceable() ?? false;
    }

    /// <summary>
    /// Returns slipperiness. 0.2 if wet, 1.0 otherwise. Spec: <c>f(IBlockAccess,x,y,z)</c>.
    /// </summary>
    public virtual float GetSlipperiness(IBlockAccess world, int x, int y, int z)
        => world.IsWet(x, y, z) ? 0.2f : 1.0f;

    /// <summary>
    /// Spawns an EntityItem at a jittered position inside block (x,y,z).
    /// Jitter: (rnd*0.7)+0.15 per axis so items appear in the centre third of the block.
    /// Pickup delay = 10 ticks (quirk 5). Spec: <c>a(ry,x,y,z,dk)</c>.
    /// </summary>
    protected void SpawnAsEntity(IWorld world, int x, int y, int z, ItemStack stack)
    {
        if (world.IsClientSide) return;

        double jx = world.Random.NextFloat() * 0.7f + 0.15;
        double jy = world.Random.NextFloat() * 0.7f + 0.15;
        double jz = world.Random.NextFloat() * 0.7f + 0.15;

        // EntityItem requires the concrete World; IWorld is the calling contract.
        // Blocks are only ever ticked by World.MainTick, so the cast is safe.
        if (world is not World concreteWorld) return;

        var entity = new EntityItem(concreteWorld, x + jx, y + jy, z + jz, stack);
        entity.PickupDelay = 10; // quirk 5 — 10-tick pickup delay
        world.SpawnEntity(entity);
    }

    /// <summary>Unlocalized name (translation key, e.g. "tile.stone"). Spec: <c>p()</c>.</summary>
    public string? GetUnlocalizedName() => BlockName;

    /// <summary>Whether this block gets random tick calls. Spec: <c>q()</c>.</summary>
    public bool IsNeedsRandomTick() => NeedsRandomTick;

    /// <summary>
    /// Light opacity. Delegates to <see cref="Material.GetMobility"/> on the block's material
    /// (Material quirk 1: mobility value doubles as light opacity source).
    /// Falls back to the cached <see cref="LightOpacity"/> value if material is null.
    /// Spec: <c>i()</c> → <c>bZ.l()</c>.
    /// </summary>
    public int GetLightOpacity()
        => BlockMaterial?.GetMobility() ?? LightOpacity[BlockID];

    // ── Drop helpers (spec §7) ────────────────────────────────────────────────

    /// <summary>
    /// Drops this block as an item with 100% chance. Spec: <c>b(World,x,y,z,meta,fortune)</c> (final).
    /// </summary>
    public void DropBlockAsItem(IWorld world, int x, int y, int z, int meta, int fortune)
        => DropBlockAsItemWithChance(world, x, y, z, meta, 1.0f, fortune);

    /// <summary>
    /// Drops this block as an item with a probability gate per dropped item.
    /// Jitter formula: (rnd*0.7)+0.15 per axis (quirk 4). Spec: <c>a(World,x,y,z,meta,float chance,fortune)</c>.
    /// </summary>
    public virtual void DropBlockAsItemWithChance(
        IWorld world, int x, int y, int z, int meta, float dropChance, int fortune)
    {
        if (world.IsClientSide) return;

        int qty = QuantityDroppedWithBonus(fortune, world.Random);
        for (int i = 0; i < qty; i++)
        {
            if (world.Random.NextFloat() > dropChance) continue;

            int itemId = IdDropped(meta, world.Random, fortune);
            if (itemId <= 0) continue;

            SpawnAsEntity(world, x, y, z, new ItemStack(itemId, 1, DamageDropped(meta)));
        }
    }

    // ── Explosion interface (spec: Explosion_Spec §4, §6) ────────────────────

    /// <summary>
    /// obf: <c>yy.a(ia sourceEntity)</c> — blast resistance used by Explosion ray attenuation.
    /// Returns <see cref="BlockResistance"/> / 5.0F per open question §4 resolution.
    /// Can be overridden (e.g. Obsidian returns a very high value).
    /// </summary>
    public virtual float GetExplosionResistance(Entity? sourceEntity) => BlockResistance / 5.0f;

    /// <summary>
    /// obf: <c>yy.i(ry, x, y, z)</c> — called when another explosion destroys this block.
    /// Default no-op; overridden by BlockTNT to chain-spawn a primed TNT.
    /// </summary>
    public virtual void OnBlockDestroyedByExplosion(IWorld world, int x, int y, int z) { }

    // ── toString ─────────────────────────────────────────────────────────────

    public override string ToString()
        => $"Block{{id={BlockID}, name={BlockName ?? "(unnamed)"}}}";
}
