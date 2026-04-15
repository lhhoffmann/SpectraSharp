using SpectraSharp.Core.TileEntity;

namespace SpectraSharp.Core.WorldGen;

/// <summary>
/// Replica of <c>acj</c> (WorldGenDungeon) — underground dungeon room generator.
/// Extends <see cref="WorldGenerator"/> (<c>ig</c>).
///
/// Generates a single rectangular dungeon room containing:
///   - Cobblestone and mossy-cobblestone walls/floor/ceiling
///   - Up to 2 loot chests (8 random loot rolls each)
///   - A mob spawner in the centre
///
/// Validation: the site is only accepted when:
///   - Floor and ceiling layers are fully solid
///   - Between 1 and 5 natural wall openings (two-block-high air gaps) are present
///
/// Quirks preserved (spec §8):
///   1. Chests silently dropped if no wall-adjacent air spot found after 3 tries.
///   2. Chest loot rolls share random slot indices — earlier items can be overwritten.
///   3. Dungeon height is always 3 (hardcoded).
///   4. Door counter counts positions where BOTH border tiles at y and y+1 are air.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldGenDungeon_Spec.md
/// </summary>
public sealed class WorldGenDungeon : WorldGenerator
{
    // ── Block IDs (spec §3 yy references) ────────────────────────────────────

    private const int AirId              = 0;
    private const int CobblestoneId      = 4;   // yy.w.bM
    private const int MossyCobblestoneId = 48;  // yy.ao.bM
    private const int ChestId            = 54;  // yy.au.bM
    private const int MobSpawnerId       = 52;  // yy.as.bM

    // ── Item IDs (spec §4 loot table — acy.xxx.bM) ───────────────────────────

    private const int SaddleId      = 329;  // acy.az
    private const int IronIngotId   = 265;  // acy.n
    private const int BreadId       = 297;  // acy.T
    private const int WheatId       = 296;  // acy.S
    private const int GunpowderId   = 289;  // acy.L
    private const int StringId      = 287;  // acy.J
    private const int BucketId      = 325;  // acy.av
    private const int GoldenAppleId = 322;  // acy.as
    private const int RedstoneDustId = 331; // acy.aB
    private const int MusicDisc13Id = 2256; // acy.bB.bM     (first music disc)
    private const int MusicDiscCatId = 2257;// acy.bB.bM + 1 (second music disc)
    // acy.aV: item ID unresolved — spec open question §9.1. Slot skipped (0 = no item).
    private const int AcyAVItemId   = 0;   // TODO: confirm acy.aV from vanilla 1.0 Item registry

    // ── Dungeon constants (spec §3, quirk 3) ─────────────────────────────────

    private const int DungeonHeight = 3; // var6 = 3, hardcoded

    // ── WorldGenerator ────────────────────────────────────────────────────────

    public override bool Generate(IWorld world, JavaRandom rand, int x, int y, int z)
    {
        int xRadius = rand.NextInt(2) + 2; // 2 or 3
        int zRadius = rand.NextInt(2) + 2; // 2 or 3

        // ── Phase 1: Site validation ──────────────────────────────────────────

        int doorCount = 0;

        for (int bx = x - xRadius - 1; bx <= x + xRadius + 1; bx++)
        for (int by = y - 1;           by <= y + DungeonHeight + 1; by++)
        for (int bz = z - zRadius - 1; bz <= z + zRadius + 1; bz++)
        {
            Material mat = world.GetBlockMaterial(bx, by, bz);

            if (by == y - 1 && !mat.IsSolid()) return false;             // floor must be solid
            if (by == y + DungeonHeight + 1 && !mat.IsSolid()) return false; // ceiling must be solid

            // Count natural wall openings (spec §3.1 / quirk 4)
            bool isWallBorder = (bx == x - xRadius - 1 || bx == x + xRadius + 1
                               || bz == z - zRadius - 1 || bz == z + zRadius + 1);
            if (isWallBorder && by == y)
            {
                bool floorTileAir = world.GetBlockId(bx, by,     bz) == AirId;
                bool aboveTileAir = world.GetBlockId(bx, by + 1, bz) == AirId;
                if (floorTileAir && aboveTileAir) doorCount++;
            }
        }

        if (doorCount < 1 || doorCount > 5) return false;

        // ── Phase 2: Room construction ────────────────────────────────────────

        for (int bx = x - xRadius - 1; bx <= x + xRadius + 1; bx++)
        for (int by = y + DungeonHeight; by >= y - 1; by--)              // top-down (spec §3.2)
        for (int bz = z - zRadius - 1; bz <= z + zRadius + 1; bz++)
        {
            bool isInterior = bx > x - xRadius - 1
                           && by > y - 1
                           && bz > z - zRadius - 1
                           && bx < x + xRadius + 1
                           && by < y + DungeonHeight + 1
                           && bz < z + zRadius + 1;

            if (isInterior)
            {
                world.SetBlock(bx, by, bz, AirId);
            }
            else if (by >= 0 && !world.GetBlockMaterial(bx, by - 1, bz).IsSolid())
            {
                // Hanging wall with no support: remove
                world.SetBlock(bx, by, bz, AirId);
            }
            else if (world.GetBlockMaterial(bx, by, bz).IsSolid())
            {
                int blockId;
                if (by == y - 1 && rand.NextInt(4) != 0)
                    blockId = MossyCobblestoneId; // 75% mossy cobblestone floor
                else
                    blockId = CobblestoneId;      // walls, ceiling, 25% floor
                world.SetBlock(bx, by, bz, blockId);
            }
        }

        // ── Phase 3: Chest placement (2 attempts, 3 tries each) ──────────────

        for (int attempt = 0; attempt < 2; attempt++)
        {
            for (int tries = 0; tries < 3; tries++)
            {
                int cx = x + rand.NextInt(xRadius * 2 + 1) - xRadius;
                int cz = z + rand.NextInt(zRadius * 2 + 1) - zRadius;

                if (world.GetBlockId(cx, y, cz) != AirId) continue;

                // Count solid neighbours on 4 horizontal faces at y
                int solidNeighbours = 0;
                if (world.GetBlockMaterial(cx - 1, y, cz    ).IsSolid()) solidNeighbours++;
                if (world.GetBlockMaterial(cx + 1, y, cz    ).IsSolid()) solidNeighbours++;
                if (world.GetBlockMaterial(cx,     y, cz - 1).IsSolid()) solidNeighbours++;
                if (world.GetBlockMaterial(cx,     y, cz + 1).IsSolid()) solidNeighbours++;

                if (solidNeighbours != 1) continue; // must be against exactly one wall

                world.SetBlock(cx, y, cz, ChestId);
                if (world.GetTileEntity(cx, y, cz) is TileEntityChest chest)
                {
                    for (int slot = 0; slot < 8; slot++)
                    {
                        ItemStack? lootItem = RollLootItem(rand);
                        if (lootItem == null) continue;
                        chest.Slots[rand.NextInt(chest.Slots.Length)] = lootItem; // quirk 2
                    }
                }
                break; // stop trying for this attempt
            }
        }

        // ── Phase 4: Mob spawner placement ────────────────────────────────────

        world.SetBlock(x, y, z, MobSpawnerId);
        if (world.GetTileEntity(x, y, z) is TileEntityMobSpawner spawner)
            spawner.EntityTypeId = RollMobType(rand);
        else
            Console.Error.WriteLine($"[WorldGenDungeon] Failed to fetch mob spawner entity at ({x},{y},{z})");

        return true;
    }

    // ── Loot table (spec §4) ─────────────────────────────────────────────────

    private static ItemStack? RollLootItem(JavaRandom rand)
    {
        return rand.NextInt(11) switch
        {
            0  => new ItemStack(SaddleId,       1),
            1  => new ItemStack(IronIngotId,    rand.NextInt(4) + 1),
            2  => new ItemStack(BreadId,        1),
            3  => new ItemStack(WheatId,        rand.NextInt(4) + 1),
            4  => new ItemStack(GunpowderId,    rand.NextInt(4) + 1),
            5  => new ItemStack(StringId,       rand.NextInt(4) + 1),
            6  => new ItemStack(BucketId,       1),
            7  => rand.NextInt(100) == 0 ? new ItemStack(GoldenAppleId, 1) : null,
            8  => rand.NextInt(2)   == 0 ? new ItemStack(RedstoneDustId, rand.NextInt(4) + 1) : null,
            9  => rand.NextInt(10)  == 0 ? new ItemStack(MusicDisc13Id + rand.NextInt(2), 1) : null,
            10 => AcyAVItemId > 0        ? new ItemStack(AcyAVItemId, 1, 3) : null, // spec open question
            _  => null,
        };
    }

    // ── Mob spawner type table (spec §5) ─────────────────────────────────────

    private static string RollMobType(JavaRandom rand) => rand.NextInt(4) switch
    {
        0 => "Skeleton",
        1 => "Zombie",
        2 => "Zombie",
        _ => "Spider",
    };
}
