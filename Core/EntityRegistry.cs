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
        Register<EntityXPOrb>("XPOrb", 2);

        // Unimplemented entities — int IDs only (no C# class yet)
        RegisterId("LeashKnot",    8);
        Register<EntityPainting>("Painting", 9);
        Register<EntityArrow>("Arrow", 10);
        Register<EntitySnowball>("Snowball", 11);
        Register<EntityFireball>("Fireball", 12);
        Register<EntitySmallFireball>("SmallFireball", 13);
        Register<EntityEnderPearl>("ThrownEnderpearl", 14);
        Register<EntityEyeOfEnder>("EyeOfEnderSignal", 15);
        RegisterId("ThrownPotion",  16);
        RegisterId("ThrownExpBottle", 17);
        RegisterId("ItemFrame",    18);
        RegisterId("WitherSkull",  19);
        Register<EntityTNTPrimed>("PrimedTnt", 20);
        Register<EntityFallingSand>("FallingSand", 21);
        RegisterId("FireworksRocketEntity", 22);
        Register<EntityMinecart>("Minecart", 40);
        Register<EntityBoat>("Boat", 41);
        RegisterId("Mob",          48);
        RegisterId("Monster",      49);
        Register<Mobs.EntityCreeper>("Creeper",    50);
        Register<Mobs.EntitySkeleton>("Skeleton", 51);
        Register<Mobs.EntitySpider>("Spider",     52);
        RegisterId("Giant",        53);
        Register<Mobs.EntityZombie>("Zombie",     54);
        Register<Mobs.EntitySlime>("Slime",        55);
        Register<Mobs.EntityGhast>("Ghast",        56);
        Register<Mobs.EntityZombiePigman>("PigZombie", 57);
        Register<Mobs.EntityEnderman>("Enderman",   58);
        Register<Mobs.EntityCaveSpider>("CaveSpider", 59);
        Register<Mobs.EntitySilverfish>("Silverfish", 60);
        RegisterId("Blaze",        61);
        Register<Mobs.EntityMagmaCube>("LavaSlime", 62);
        Register<EntityDragon>("EnderDragon",  63);
        RegisterId("WitherBoss",   64);
        RegisterId("Bat",          65);
        RegisterId("Witch",        66);
        Register<Mobs.EntityPig>("Pig",         90);
        Register<Mobs.EntitySheep>("Sheep",     91);
        Register<Mobs.EntityCow>("Cow",         92);
        Register<Mobs.EntityChicken>("Chicken", 93);
        Register<Mobs.EntitySquid>("Squid",        94);
        Register<Mobs.EntityWolf>("Wolf",          95);
        Register<Mobs.EntityMooshroom>("MushroomCow", 96);
        Register<Mobs.EntitySnowMan>("SnowMan",    97);
        RegisterId("Ozelot",       98);
        RegisterId("VillagerGolem",99);
        Register<Mobs.EntityVillager>("Villager", 120);
        Register<EntityEnderCrystal>("EnderCrystal", 200);
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
