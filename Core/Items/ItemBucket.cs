using SpectraEngine.Core.Mobs;

namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>en</c> (ItemBucket) — empty, water, and lava bucket variants.
///
/// Field <c>a</c> (LiquidBlockId):
///   0  = empty bucket
///   9  = water bucket (still water block ID)
///   11 = lava bucket (still lava block ID)
///
/// Item IDs:
///   325 = empty bucket  (itemId 69)
///   326 = water bucket  (itemId 70)
///   327 = lava bucket   (itemId 71)
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemBucket_Spec.md §2–3
/// </summary>
public sealed class ItemBucket : Item
{
    // Block IDs for still liquids
    private const int BlockStillWater = 9;
    private const int BlockStillLava  = 11;

    // Item registry indices for the three bucket types
    private const int EmptyBucketId = 325;  // 256+69
    private const int WaterBucketId = 326;  // 256+70
    private const int LavaBucketId  = 327;  // 256+71
    private const int MilkBucketId  = 335;  // 256+79

    /// <summary>obf: <c>a</c> — liquid block ID, 0 for empty.</summary>
    private readonly int _liquidBlockId;

    public ItemBucket(int itemId, int liquidBlockId) : base(itemId)
    {
        MaxStackSize    = 1;
        _liquidBlockId  = liquidBlockId;
    }

    // ── Right-click (spec §3) ─────────────────────────────────────────────────

    /// <summary>
    /// obf: <c>c(dk, ry, vi)</c> — ray-cast and pick up / place liquid.
    /// </summary>
    public override ItemStack OnItemRightClick(ItemStack stack, World world, object player)
    {
        if (player is not EntityPlayer ep) return stack;

        // Ray-cast from player eye position
        var hit = GetPlayerLookRayTrace(world, ep, allowWater: _liquidBlockId == 0);
        if (hit == null) return stack;

        if (hit.Type == HitType.Tile)
        {
            int bx = hit.BlockX, by = hit.BlockY, bz = hit.BlockZ;

            if (_liquidBlockId == 0)
            {
                // Empty bucket — pick up liquid
                var mat = world.GetBlockMaterial(bx, by, bz);
                int meta = world.GetBlockMetadata(bx, by, bz);

                if (mat == Material.Water && meta == 0)
                {
                    world.SetBlock(bx, by, bz, 0);
                    if (ep.Abilities.Invulnerable) return stack; // creative
                    return ConsumeAndReturn(stack, ep, WaterBucketId);
                }
                if (mat == Material.Lava_ && meta == 0)
                {
                    world.SetBlock(bx, by, bz, 0);
                    if (ep.Abilities.Invulnerable) return stack; // creative
                    return ConsumeAndReturn(stack, ep, LavaBucketId);
                }
            }
            else
            {
                // Full bucket — place liquid (spec §3.1 face offset)
                int px = bx, py = by, pz = bz;
                switch (hit.FaceId)
                {
                    case 0: py--; break;
                    case 1: py++; break;
                    case 2: pz--; break;
                    case 3: pz++; break;
                    case 4: px--; break;
                    case 5: px++; break;
                }

                if (CanPlaceAt(world, px, py, pz))
                {
                    if (!world.IsClientSide && world.IsNether && _liquidBlockId == BlockStillLava)
                    {
                        // lava in water context: play sizzle (stub — sound system pending)
                    }
                    world.SetBlock(px, py, pz, _liquidBlockId);
                    if (ep.Abilities.Invulnerable) return stack; // creative
                    return ConsumeAndReturn(stack, ep, EmptyBucketId);
                }
            }
        }
        else if (hit.Type == HitType.Entity && _liquidBlockId == 0)
        {
            // Empty bucket + cow = milk (spec §3.2)
            if (hit.Entity is EntityCow)
                return ConsumeAndReturn(stack, ep, MilkBucketId);
        }

        return stack;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Ray-trace from player eye position 5 blocks in look direction.</summary>
    private static MovingObjectPosition? GetPlayerLookRayTrace(World world, EntityPlayer player, bool allowWater)
    {
        double eyeY = player.PosY + player.PlayerEyeHeight;
        double yaw   = player.RotationYaw   * Math.PI / 180.0;
        double pitch = player.RotationPitch * Math.PI / 180.0;

        double sinYaw   = Math.Sin(yaw);
        double cosYaw   = Math.Cos(yaw);
        double sinPitch = Math.Sin(pitch);
        double cosPitch = Math.Cos(pitch);

        double dx = -sinYaw * cosPitch;
        double dy = -sinPitch;
        double dz =  cosYaw * cosPitch;

        const double reach = 5.0;
        var from = Vec3.GetFromPool(player.PosX, eyeY, player.PosZ);
        var to   = Vec3.GetFromPool(player.PosX + dx * reach, eyeY + dy * reach, player.PosZ + dz * reach);

        return world.RayTraceBlocks(from, to);
    }

    /// <summary>Returns true if the block at (x,y,z) can be replaced by placing a liquid.</summary>
    private static bool CanPlaceAt(World world, int x, int y, int z)
    {
        int id = world.GetBlockId(x, y, z);
        if (id == 0) return true; // air
        var block = Block.BlocksList[id];
        if (block == null) return false;
        return !block.IsOpaqueCube() && !block.RenderAsNormalBlock();
    }

    /// <summary>Decrements stack by 1 and returns item with <paramref name="returnItemId"/>, or the new stack.</summary>
    private static ItemStack ConsumeAndReturn(ItemStack stack, EntityPlayer player, int returnItemId)
    {
        stack.StackSize--;
        var result = new ItemStack(returnItemId);
        if (stack.StackSize <= 0)
            return result;
        player.Inventory.AddItemStackToInventory(result);
        return stack;
    }
}

/// <summary>
/// Replica of <c>om</c> (ItemMilkBucket) — clears all active potion effects.
///
/// Item ID: 335 (itemId=79). Stack size 1.
/// Drink action clears all effects and returns empty bucket (ID 325).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemBucket_Spec.md §4
/// </summary>
public sealed class ItemMilkBucket : Item
{
    private const int EmptyBucketId = 325;

    public ItemMilkBucket() : base(79) // 256+79 = 335
    {
        MaxStackSize = 1;
        SetUnlocalizedName("milkBucket");
    }

    public override int GetMaxItemUseDuration(ItemStack stack) => 32;

    public override ItemStack OnItemRightClick(ItemStack stack, World world, object player)
    {
        if (player is EntityPlayer ep)
            ep.StartUsingItem(stack, 32);
        return stack;
    }

    public override ItemStack FinishUsingItem(ItemStack stack, World world, object player)
    {
        if (player is EntityPlayer ep && !world.IsClientSide)
        {
            // Clear ALL active potion effects (spec §4 OQ 5.2: all effects removed)
            ep.ClearAllPotionEffects();
        }

        stack.StackSize--;
        if (stack.StackSize <= 0)
            return new ItemStack(EmptyBucketId);

        if (player is EntityPlayer ep2)
            ep2.Inventory.AddItemStackToInventory(new ItemStack(EmptyBucketId));
        return stack;
    }
}
