namespace SpectraEngine.Core.TileEntity;

/// <summary>
/// Abstract base for all tile entities. Replica of <c>bq</c>.
///
/// A TileEntity is a block-position-bound data store and optional ticker
/// associated with specific blocks (Furnace, Chest, etc.).
///
/// Save/load chain:
///   <see cref="WriteToNbt"/> calls <see cref="WriteTileEntityToNbt"/> (override in subclass).
///   <see cref="ReadFromNbt"/> calls <see cref="ReadTileEntityFromNbt"/> (override in subclass).
///   <see cref="Create"/> is the static factory.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/TileEntity_Spec.md §3
/// </summary>
public abstract class TileEntity
{
    // ── Static registry (spec §3.2) ───────────────────────────────────────────

    private static readonly Dictionary<string, Func<TileEntity>> _factories = [];
    private static readonly Dictionary<Type,   string>           _typeToId  = [];

    // ── Block-ID → TE factory (spec: auto-creation on block placement) ─────────
    private static readonly Dictionary<int, Func<TileEntity>> _blockIdFactories = new()
    {
        { 23, () => new TileEntityDispenser()  },  // dispenser
        { 52, () => new TileEntityMobSpawner() },  // mob spawner
        { 54, () => new TileEntityChest()      },  // chest
        { 61, () => new TileEntityFurnace()    },  // furnace (off)
        { 62, () => new TileEntityFurnace()    },  // furnace (on)
        { 63, () => new TileEntitySign()       },  // standing sign
        { 68, () => new TileEntitySign()       },  // wall sign
        { 25, () => new TileEntityNote()       },  // note block
        { 36, () => new TileEntityPiston()     },  // moving piston (blank — replaced by SetTileEntity)
        { 84, () => new TileEntityJukebox()    },  // jukebox
    };

    /// <summary>
    /// Creates the tile entity that belongs to <paramref name="blockId"/>,
    /// or returns null if the block has no tile entity.
    /// Called by <see cref="Chunk"/> when a block is placed.
    /// </summary>
    public static TileEntity? CreateForBlock(int blockId)
        => _blockIdFactories.TryGetValue(blockId, out var f) ? f() : null;

    static TileEntity()
    {
        Register<TileEntityFurnace>  ("Furnace");
        Register<TileEntityChest>    ("Chest");
        Register<TileEntityDispenser>("Trap");
        Register<TileEntitySign>     ("Sign");
        Register<TileEntityMobSpawner>("MobSpawner");
        Register<TileEntityNote>     ("Music");
        // Remaining registered TEs — stub (no additional NBT):
        Register<TileEntityPiston>         ("Piston");
        Register<TileEntityBrewingStand>   ("Cauldron");
        Register<TileEntityEnchantTable>   ("EnchantTable");
        Register<TileEntityJukebox>        ("RecordPlayer");
        Register<TileEntityEndPortal>      ("Airportal");
    }

    private static void Register<T>(string id) where T : TileEntity, new()
    {
        _factories[id]      = () => new T();
        _typeToId[typeof(T)] = id;
    }

    // ── Instance fields (spec §3.1) ───────────────────────────────────────────

    public World? World;       // obf: c
    public int    X;           // obf: d
    public int    Y;           // obf: e
    public int    Z;           // obf: f
    public bool   IsInvalid;   // obf: g — to-remove flag; set by Invalidate()
    private int   _cachedBlockId = -1; // obf: h
    private Block? _cachedBlock;       // obf: i

    // ── Tick hook (spec §3.6 b()) ─────────────────────────────────────────────

    /// <summary>Per-tick logic. No-op in base; overridden by Furnace and MobSpawner.</summary>
    public virtual void Tick() { }

    // ── NBT: base write (spec §3.3) ───────────────────────────────────────────

    /// <summary>
    /// Writes this TE to <paramref name="tag"/>. Writes "id", "x", "y", "z" then
    /// dispatches to <see cref="WriteTileEntityToNbt"/>.
    /// Spec: <c>bq.a(ik tag)</c>.
    /// </summary>
    public void WriteToNbt(Nbt.NbtCompound tag)
    {
        if (!_typeToId.TryGetValue(GetType(), out string? id))
            throw new InvalidOperationException($"TileEntity {GetType().Name} is not registered.");
        tag.PutString("id", id);
        tag.PutInt("x", X);
        tag.PutInt("y", Y);
        tag.PutInt("z", Z);
        WriteTileEntityToNbt(tag);
    }

    /// <summary>
    /// Reads coordinates from <paramref name="tag"/> then dispatches to
    /// <see cref="ReadTileEntityFromNbt"/>. Spec: <c>bq.b(ik tag)</c>.
    /// </summary>
    public void ReadFromNbt(Nbt.NbtCompound tag)
    {
        X = tag.GetInt("x");
        Y = tag.GetInt("y");
        Z = tag.GetInt("z");
        ReadTileEntityFromNbt(tag);
    }

    // ── Factory (spec §3.5) ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a TE from a TAG_Compound read from chunk NBT.
    /// Returns null and prints a message for unknown IDs (spec §3.5 behaviour).
    /// Spec: <c>bq.c(ik tag)</c>.
    /// </summary>
    public static TileEntity? Create(Nbt.NbtCompound tag)
    {
        string id = tag.GetString("id");
        if (!_factories.TryGetValue(id, out var factory))
        {
            Console.WriteLine($"[TileEntity] Skipping TileEntity with id {id}");
            return null;
        }
        TileEntity te = factory();
        te.ReadFromNbt(tag);
        return te;
    }

    // ── Cache helpers (spec §3.6) ──────────────────────────────────────────────

    /// <summary>Returns cached block ID, reading from world if needed. Spec: <c>f()</c>.</summary>
    public int GetBlockId()
    {
        if (_cachedBlockId == -1 && World != null)
            _cachedBlockId = World.GetBlockId(X, Y, Z);
        return _cachedBlockId;
    }

    /// <summary>Returns cached Block, reading from world if needed. Spec: <c>g()</c>.</summary>
    public Block? GetBlock()
    {
        int id = GetBlockId();
        if (_cachedBlock == null && id >= 0 && id < Block.BlocksList.Length)
            _cachedBlock = Block.BlocksList[id];
        return _cachedBlock;
    }

    /// <summary>Invalidates the TE (marks for removal). Spec: <c>l()</c>.</summary>
    public void Invalidate() => IsInvalid = true;

    /// <summary>Un-invalidates. Spec: <c>m()</c>.</summary>
    public void Validate() => IsInvalid = false;

    /// <summary>Clears cached block/ID. Spec: <c>n()</c>.</summary>
    public void ClearCache() { _cachedBlock = null; _cachedBlockId = -1; }

    /// <summary>Marks dirty and notifies world. Spec: <c>h()</c>.</summary>
    public void MarkDirty()
    {
        if (World != null)
            World.GetChunkFromBlockCoords(X, Z)?.MarkDirty();
        ClearCache();
    }

    /// <summary>Squared distance to given world point. Spec: <c>a(double, double, double)</c>.</summary>
    public double DistanceSq(double px, double py, double pz)
    {
        double dx = X + 0.5 - px;
        double dy = Y + 0.5 - py;
        double dz = Z + 0.5 - pz;
        return dx * dx + dy * dy + dz * dz;
    }

    // ── Abstract NBT hooks ────────────────────────────────────────────────────

    /// <summary>
    /// Subclass writes its own NBT fields after the base writes id/x/y/z.
    /// Spec: the part of each subclass's <c>a(ik tag)</c> beyond <c>super.a(tag)</c>.
    /// </summary>
    protected virtual void WriteTileEntityToNbt(Nbt.NbtCompound tag) { }

    /// <summary>
    /// Subclass reads its own NBT fields after the base reads x/y/z.
    /// Spec: the part of each subclass's <c>b(ik tag)</c> beyond <c>super.b(tag)</c>.
    /// </summary>
    protected virtual void ReadTileEntityFromNbt(Nbt.NbtCompound tag) { }

    // ── Inventory slot helpers (spec §4 — shared by Chest/Furnace/Dispenser) ──

    /// <summary>
    /// Writes a slot array to a sparse TAG_List under key "Items".
    /// Only non-null slots are written; each gets a "Slot" byte header.
    /// Spec: §4 write pattern.
    /// </summary>
    protected static void WriteSlots(Nbt.NbtCompound tag, ItemStack?[] slots, int count)
    {
        var list = new Nbt.NbtList();
        for (int i = 0; i < count; i++)
        {
            if (slots[i] == null) continue;
            var slot = new Nbt.NbtCompound();
            slot.PutByte("Slot", (byte)i);
            slots[i]!.SaveToNbt(slot);
            list.Add(slot);
        }
        tag.PutList("Items", list);
    }

    /// <summary>
    /// Reads sparse "Items" TAG_List into a slot array.
    /// <paramref name="mask"/> is true for Chest/Dispenser (reads slot &amp; 255),
    /// false for Furnace (signed byte, no mask).
    /// Spec: §4 read pattern.
    /// </summary>
    protected static void ReadSlots(Nbt.NbtCompound tag, ItemStack?[] slots, bool unsignedSlot = true)
    {
        Nbt.NbtList? list = tag.GetList("Items");
        if (list == null) return;
        for (int i = 0; i < list.Count; i++)
        {
            var entry = (Nbt.NbtCompound)list[i];
            int slot = unsignedSlot
                ? (entry.GetByte("Slot") & 255)
                : (sbyte)entry.GetByte("Slot");
            if (slot < 0 || slot >= slots.Length) continue;
            slots[slot] = ItemStack.LoadFromNbt(entry);
        }
    }
}
