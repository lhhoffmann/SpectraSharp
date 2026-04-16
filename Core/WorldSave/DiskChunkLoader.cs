using SpectraEngine.Core.Nbt;

namespace SpectraEngine.Core.WorldSave;

/// <summary>
/// Per-chunk .dat file reader/writer. Replica of <c>gy</c> (ChunkLoader).
/// One instance per dimension directory.
///
/// File path formula (spec §8):
///   subX = base36(chunkX &amp; 63);  subZ = base36(chunkZ &amp; 63)
///   file = &lt;dir&gt; / subX / subZ / c.&lt;x36&gt;.&lt;z36&gt;.dat
///
/// Chunk NBT format: spec §9.
/// Load algorithm:  spec §10.
/// Save algorithm:  spec §11.
///
/// Entity serialization uses <see cref="EntityRegistry.CreateFromNbt"/> (load) and
/// <see cref="Entity.SaveToNbt"/> (save). TileEntity serialization uses
/// <see cref="TileEntity.TileEntity.Create"/> and <see cref="TileEntity.TileEntity.WriteToNbt"/>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldSave_Spec.md §8–§12
/// </summary>
public sealed class DiskChunkLoader : IChunkPersistence
{
    private readonly string _chunkDir;  // directory this loader writes chunks into
    private readonly string _worldDir;  // world root (tmp_chunk.dat lives here)

    public DiskChunkLoader(string chunkDir, string worldDir)
    {
        _chunkDir = chunkDir;
        _worldDir = worldDir;
    }

    // ── IChunkPersistence ─────────────────────────────────────────────────────

    public void PostSave(World world, Chunk chunk) { }
    public void Flush()                            { }
    public void Close()                            { }

    // ── Load (spec §10) ───────────────────────────────────────────────────────

    /// <summary>
    /// Loads a chunk from its .dat file. Returns null if not yet saved.
    /// Spec: <c>gy.a(ry, int, int)</c>.
    /// </summary>
    public Chunk? LoadChunk(World world, int chunkX, int chunkZ)
    {
        string path = ChunkPath(chunkX, chunkZ);
        if (!File.Exists(path)) return null;

        NbtCompound root;
        try
        {
            root = NbtIo.Read(path);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ChunkLoader] Failed to read {path}: {ex.Message}");
            return null;
        }

        if (!root.HasKey("Level"))
        {
            Console.Error.WriteLine($"[ChunkLoader] Chunk file missing 'Level' tag: {path}");
            return null;
        }

        NbtCompound? level = root.GetCompound("Level");
        if (level == null || !level.HasKey("Blocks"))
        {
            Console.Error.WriteLine($"[ChunkLoader] Chunk file missing 'Blocks' in Level: {path}");
            return null;
        }

        Chunk chunk = DeserializeChunk(world, level);

        // Chunk coord mismatch recovery (spec §10 step 7)
        if (chunk.ChunkX != chunkX || chunk.ChunkZ != chunkZ)
        {
            Console.Error.WriteLine(
                $"[ChunkLoader] Chunk coord mismatch — file says ({chunk.ChunkX},{chunk.ChunkZ}), " +
                $"expected ({chunkX},{chunkZ}). Patching in-memory; will re-save on next flush.");
            // Rebuild with corrected coords — create new chunk with same data
            chunk = RebuildWithCoords(world, level, chunkX, chunkZ);
        }

        chunk.IsLoaded = true;
        return chunk;
    }

    private static Chunk DeserializeChunk(World world, NbtCompound level)
    {
        int cx = level.GetInt("xPos");
        int cz = level.GetInt("zPos");

        byte[] blockIds = level.GetByteArray("Blocks");

        // Use the 4-arg constructor which takes the block data directly
        var chunk = new Chunk(world, blockIds, cx, cz);

        // Metadata (spec §10.1 — reset to zeroes if empty/invalid)
        byte[] metaData = level.GetByteArray("Data");
        if (metaData.Length > 0 && HasNonZero(metaData))
            chunk.MetadataRaw.SetRawData(metaData);

        // SkyLight
        byte[] skyData = level.GetByteArray("SkyLight");
        if (skyData.Length > 0 && HasNonZero(skyData))
            chunk.SkyLightRaw.SetRawData(skyData);

        // BlockLight
        byte[] blkData = level.GetByteArray("BlockLight");
        if (blkData.Length > 0 && HasNonZero(blkData))
            chunk.BlockLightRaw.SetRawData(blkData);

        // HeightMap
        byte[] heightData = level.GetByteArray("HeightMap");
        if (heightData.Length == 256)
            chunk.HeightMapRaw = heightData;

        // Recalculate skylight / heightmap if missing (spec §10.1)
        if (heightData.Length != 256 || !HasNonZero(skyData))
        {
            chunk.ClearHeightMap();
            chunk.GenerateSkylightMap();
        }

        chunk.IsPopulated       = level.GetBoolean("TerrainPopulated");
        chunk.LastSaveTime      = level.GetLong("LastUpdate");

        // Entities (EntityNBT_Spec §3–§8)
        NbtList? entitiesList = level.GetList("Entities");
        if (entitiesList != null)
        {
            foreach (var item in entitiesList.Items)
            {
                if (item is not NbtCompound entityTag) continue;
                Entity? entity = EntityRegistry.CreateFromNbt(entityTag, world);
                if (entity == null) continue;
                chunk.AddEntity(entity);
            }
        }

        // TileEntities (TileEntity_Spec §3)
        NbtList? teList = level.GetList("TileEntities");
        if (teList != null)
        {
            foreach (var item in teList.Items)
            {
                if (item is not NbtCompound teTag) continue;
                TileEntity.TileEntity? te = TileEntity.TileEntity.Create(teTag);
                if (te == null) continue;
                te.World = world;
                chunk.AddTileEntity(te.X & 15, te.Y, te.Z & 15, te);
            }
        }
        // (end tile entities)

        // TileTicks (spec §10.1 final block)
        NbtList? tileTicks = level.GetList("TileTicks");
        if (tileTicks != null)
        {
            foreach (var item in tileTicks.Items)
            {
                if (item is not NbtCompound tick) continue;
                int tx = tick.GetInt("x");
                int ty = tick.GetInt("y");
                int tz = tick.GetInt("z");
                int id = tick.GetInt("i");
                int t  = tick.GetInt("t");
                world.ScheduleBlockUpdateFromLoad(tx, ty, tz, id, t);
            }
        }

        return chunk;
    }

    private static Chunk RebuildWithCoords(World world, NbtCompound level, int cx, int cz)
    {
        // Patch the compound in-memory and re-deserialize
        level.PutInt("xPos", cx);
        level.PutInt("zPos", cz);
        return DeserializeChunk(world, level);
    }

    // ── Save (spec §11) ───────────────────────────────────────────────────────

    /// <summary>
    /// Saves a chunk to disk via tmp_chunk.dat → target rename.
    /// Spec: <c>gy.a(ry, zx)</c>.
    /// </summary>
    public void SaveChunk(World world, Chunk chunk)
    {
        world.VerifySessionLock();

        string target = ChunkPath(chunk.ChunkX, chunk.ChunkZ);

        // Subtract old file size from SizeOnDisk counter (spec §11 step 3)
        if (File.Exists(target) && world.WorldInfo != null)
            world.WorldInfo.SizeOnDisk -= new FileInfo(target).Length;

        // Build chunk NBT
        var level = new NbtCompound();
        SerializeChunk(world, chunk, level);

        var root = new NbtCompound();
        root.PutCompound("Level", level);

        // Write to tmp, rename to target (spec §11 steps 4–6)
        string tmp = Path.Combine(_worldDir, "tmp_chunk.dat");
        using (var fs = File.Create(tmp))
            NbtIo.WriteStream(root, new System.IO.Compression.GZipStream(
                fs, System.IO.Compression.CompressionLevel.Optimal));

        // Ensure target directory exists (lazy creation)
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        if (File.Exists(target)) File.Delete(target);
        File.Move(tmp, target);

        // Add new file size to SizeOnDisk counter (spec §11 step 7)
        if (world.WorldInfo != null)
            world.WorldInfo.SizeOnDisk += new FileInfo(target).Length;

        chunk.IsModified  = false;
        chunk.LastSaveTime = world.WorldTime;
    }

    private static void SerializeChunk(World world, Chunk chunk, NbtCompound level)
    {
        level.PutInt("xPos",       chunk.ChunkX);
        level.PutInt("zPos",       chunk.ChunkZ);
        level.PutLong("LastUpdate", world.WorldTime);
        level.PutByteArray("Blocks",    chunk.BlockIdsRaw);
        level.PutByteArray("Data",      chunk.MetadataRaw.GetData());
        level.PutByteArray("SkyLight",  chunk.SkyLightRaw.GetData());
        level.PutByteArray("BlockLight",chunk.BlockLightRaw.GetData());
        level.PutByteArray("HeightMap", chunk.HeightMapRaw);
        level.PutBoolean("TerrainPopulated", chunk.IsPopulated);

        // Entities (EntityNBT_Spec §3 ia.c gate)
        var entitiesTag = new NbtList();
        foreach (Entity entity in chunk.GetAllEntities())
        {
            var entityTag = new NbtCompound();
            if (entity.SaveToNbt(entityTag))
                entitiesTag.Add(entityTag);
        }
        chunk.HasEntities = entitiesTag.Count > 0;
        level.PutList("Entities", entitiesTag);

        // TileEntities (TileEntity_Spec §3)
        var tileEntitiesTag = new NbtList();
        foreach (TileEntity.TileEntity te in chunk.GetTileEntities())
        {
            if (te.IsInvalid) continue;
            var teTag = new NbtCompound();
            te.WriteToNbt(teTag);
            tileEntitiesTag.Add(teTag);
        }
        level.PutList("TileEntities", tileEntitiesTag);

        // TileTicks — only write if there are pending ticks in this chunk (spec §9.1)
        var pendingTicks = world.GetPendingTicksInChunk(chunk.ChunkX, chunk.ChunkZ, consume: false);
        if (pendingTicks.Count > 0)
        {
            var tileTicksTag = new NbtList();
            foreach (var tick in pendingTicks)
            {
                var t = new NbtCompound();
                t.PutInt("i", tick.BlockId);
                t.PutInt("x", tick.X);
                t.PutInt("y", tick.Y);
                t.PutInt("z", tick.Z);
                t.PutInt("t", (int)Math.Max(0, tick.FireTime - world.WorldTime));
                tileTicksTag.Add(t);
            }
            level.PutList("TileTicks", tileTicksTag);
        }
    }

    // ── Path helpers (spec §8) ────────────────────────────────────────────────

    private string ChunkPath(int cx, int cz)
    {
        string subX = ToBase36(cx & 63);
        string subZ = ToBase36(cz & 63);
        string file = $"c.{ToBase36(cx)}.{ToBase36(cz)}.dat";
        return Path.Combine(_chunkDir, subX, subZ, file);
    }

    /// <summary>
    /// Converts a signed integer to base-36 exactly as Java's <c>Integer.toString(n, 36)</c>.
    /// Negative numbers get a leading '-'; zero → "0".
    /// </summary>
    private static string ToBase36(int value)
    {
        if (value == 0) return "0";

        const string digits = "0123456789abcdefghijklmnopqrstuvwxyz";
        bool negative = value < 0;
        long abs = negative ? -(long)value : (long)value; // handle int.MinValue

        var chars = new System.Text.StringBuilder();
        while (abs > 0)
        {
            chars.Insert(0, digits[(int)(abs % 36)]);
            abs /= 36;
        }
        if (negative) chars.Insert(0, '-');
        return chars.ToString();
    }

    // ── Utility ───────────────────────────────────────────────────────────────

    private static bool HasNonZero(byte[] data)
    {
        foreach (byte b in data)
            if (b != 0) return true;
        return false;
    }
}
