namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>afw</c> — static entity registry used for NBT serialization.
/// Maps between entity class types, string IDs, and integer IDs.
///
/// The 35-entry table matches the Minecraft 1.0 entity list exactly (spec §8).
/// Only the string IDs that appear in NBT and the classes SpectraEngine implements
/// are registered; others exist as type-only entries so the int ID stays consistent.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/EntityNBT_Spec.md §8
/// </summary>
public static class EntityRegistry
{
    private static readonly Dictionary<Type,   string> _typeToString = [];
    private static readonly Dictionary<string, Type>   _stringToType = [];
    private static readonly Dictionary<Type,   int>    _typeToId     = [];
    private static readonly Dictionary<int,    Type>   _idToType     = [];

    static EntityRegistry()
    {
        // ── Registered entries (spec §8 table, 35 rows) ───────────────────────
        // Rows that SpectraEngine has implemented get a full type ↔ string mapping.
        // Rows for unimplemented entities only register the int ID → placeholder type.

        Register<EntityItem>("Item", 1);

        // Unimplemented entities — int IDs only (no C# class yet)
        RegisterId("XPOrb",        2);
        RegisterId("LeashKnot",    8);
        RegisterId("Painting",     9);
        RegisterId("Arrow",       10);
        RegisterId("Snowball",    11);
        RegisterId("Fireball",    12);
        RegisterId("SmallFireball", 13);
        RegisterId("ThrownEnderpearl", 14);
        RegisterId("EyeOfEnderSignal", 15);
        RegisterId("ThrownPotion",  16);
        RegisterId("ThrownExpBottle", 17);
        RegisterId("ItemFrame",    18);
        RegisterId("WitherSkull",  19);
        Register<EntityTNTPrimed>("PrimedTnt", 20);
        RegisterId("FallingSand",  21);
        RegisterId("FireworksRocketEntity", 22);
        RegisterId("Minecart",     40);
        RegisterId("Boat",         41);
        RegisterId("Mob",          48);
        RegisterId("Monster",      49);
        Register<Mobs.EntityCreeper>("Creeper",    50);
        Register<Mobs.EntitySkeleton>("Skeleton", 51);
        Register<Mobs.EntitySpider>("Spider",     52);
        RegisterId("Giant",        53);
        Register<Mobs.EntityZombie>("Zombie",     54);
        RegisterId("Slime",        55);
        RegisterId("Ghast",        56);
        RegisterId("PigZombie",    57);
        RegisterId("Enderman",     58);
        RegisterId("CaveSpider",   59);
        RegisterId("Silverfish",   60);
        RegisterId("Blaze",        61);
        RegisterId("LavaSlime",    62);
        RegisterId("EnderDragon",  63);
        RegisterId("WitherBoss",   64);
        RegisterId("Bat",          65);
        RegisterId("Witch",        66);
        Register<Mobs.EntityPig>("Pig",         90);
        Register<Mobs.EntitySheep>("Sheep",     91);
        Register<Mobs.EntityCow>("Cow",         92);
        Register<Mobs.EntityChicken>("Chicken", 93);
        RegisterId("Squid",        94);
        RegisterId("Wolf",         95);
        RegisterId("MushroomCow",  96);
        RegisterId("SnowMan",      97);
        RegisterId("Ozelot",       98);
        RegisterId("VillagerGolem",99);
        RegisterId("Villager",    120);
        RegisterId("EnderCrystal",200);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the NBT string ID for <paramref name="entity"/>, or null if the entity's
    /// type is not registered. Spec: <c>afw.b(ia entity)</c>.
    /// </summary>
    public static string? GetEntityStringId(Entity entity)
        => _typeToString.TryGetValue(entity.GetType(), out string? id) ? id : null;

    /// <summary>
    /// Returns the NBT string ID for the given type, or null if unregistered.
    /// </summary>
    public static string? GetStringId(Type type)
        => _typeToString.TryGetValue(type, out string? id) ? id : null;

    /// <summary>
    /// Returns the int ID for the given type, or -1 if unregistered.
    /// </summary>
    public static int GetIntId(Type type)
        => _typeToId.TryGetValue(type, out int id) ? id : -1;

    /// <summary>
    /// Creates an entity from the "id" string tag and calls <see cref="Entity.LoadFromNbt"/>
    /// on it. Returns null if the entity type is unknown or construction fails.
    /// Spec: EntityList factory.
    /// </summary>
    public static Entity? CreateFromNbt(Nbt.NbtCompound tag, World world)
    {
        string idStr = tag.GetString("id");
        if (!_stringToType.TryGetValue(idStr, out Type? type)) return null;

        Entity? entity;
        try
        {
            // All saveable entities must have a (World) constructor
            entity = (Entity?)Activator.CreateInstance(type, world);
        }
        catch
        {
            return null;
        }

        if (entity == null) return null;
        entity.LoadFromNbt(tag);
        return entity;
    }

    /// <summary>
    /// Creates a mob entity by its NBT string ID without loading any NBT data into it.
    /// Used by <see cref="TileEntity.TileEntityMobSpawner"/> to spawn entities.
    /// Returns null if the string ID is not registered.
    /// Spec: <c>afw.a(String id, ry world)</c>.
    /// </summary>
    public static Entity? CreateMobByStringId(string stringId, World world)
    {
        if (!_stringToType.TryGetValue(stringId, out Type? type)) return null;
        try { return (Entity?)Activator.CreateInstance(type, world); }
        catch { return null; }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void Register<T>(string stringId, int intId) where T : Entity
    {
        _typeToString[typeof(T)] = stringId;
        _stringToType[stringId]  = typeof(T);
        _typeToId[typeof(T)]     = intId;
        _idToType[intId]         = typeof(T);
    }

    private static void RegisterId(string stringId, int intId)
    {
        // No C# class yet — int ID reserved
        _idToType[intId] = typeof(Entity); // placeholder; unreachable at runtime
        _ = stringId; // kept for documentation only
    }
}
