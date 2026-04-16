namespace SpectraEngine.ModTranspiler.Mappings;

/// <summary>
/// Hardcoded Java → C# API translation table.
/// Machine-readable version of Documentation/Mods/Mappings/vanilla_api.md.
/// Used by Translator (Phase 4) to rewrite API calls line-by-line.
/// </summary>
static class VanillaApiMap
{
    /// <summary>
    /// Method call patterns. '?' is a placeholder for any single argument.
    /// Matched against decompiled Java lines using regex expansion.
    /// </summary>
    public static readonly Dictionary<string, string> MethodCalls = new()
    {
        // ── World / Block ────────────────────────────────────────────────────
        ["world.getBlockId(?,?,?)"]                    = "world.GetBlockId(?,?,?)",
        ["world.setBlock(?,?,?,?)"]                    = "world.SetBlock(?,?,?,?)",
        ["world.setBlockWithNotify(?,?,?,?)"]          = "world.SetBlockNotify(?,?,?,?)",
        ["world.getBlockMetadata(?,?,?)"]              = "world.GetBlockMeta(?,?,?)",
        ["world.setBlockMetadataWithNotify(?,?,?,?)"]  = "world.SetBlockMeta(?,?,?,?)",
        ["world.isAirBlock(?,?,?)"]                    = "world.IsAir(?,?,?)",
        ["world.markBlocksDirty(?,?,?,?,?,?)"]         = "world.MarkDirty(?,?,?,?,?,?)",
        ["world.scheduleBlockUpdate(?,?,?,?,?)"]       = "world.ScheduleTick(?,?,?,?,?)",
        ["world.getBlockLightValue(?,?,?)"]            = "world.GetLightLevel(?,?,?)",
        ["world.canSeeSky(?,?,?)"]                     = "world.CanSeeSky(?,?,?)",
        ["world.isBlockNormalCube(?,?,?)"]             = "world.IsNormalCube(?,?,?)",

        // ── Entity / Player ──────────────────────────────────────────────────
        ["player.hurtPlayer(?,?)"]                     = "player.Hurt(?,?)",
        ["player.sendChatMessage(?)"]                  = "player.SendChat(?)",
        ["player.addPotionEffect(?)"]                  = "player.AddEffect(?)",
        ["player.inventory.addItemStackToInventory(?)"]= "player.Inventory.TryAdd(?)",
        ["entity.setDead()"]                           = "entity.SetDead()",
        ["entity.setPosition(?,?,?)"]                  = "entity.SetPosition(?,?,?)",

        // ── Sound ────────────────────────────────────────────────────────────
        ["world.playSoundAtEntity(?,?,?,?)"]           = "Audio.PlayAt(entity.Position,?,?,?)",
        ["world.playSoundEffect(?,?,?,?,?,?)"]         = "Audio.PlayAt(?,?,?,?,?,?)",

        // ── Crafting ─────────────────────────────────────────────────────────
        ["CraftingManager.getInstance().addRecipe(?)"] = "crafting.AddShapedRecipe(?)",
        ["CraftingManager.getInstance().addShapelessRecipe(?)"] = "crafting.AddShapelessRecipe(?)",
        ["FurnaceRecipes.smelting().addSmelting(?,?,?)"] = "smelting.AddSmeltingRecipe(?,?,?)",

        // ── Block registry ───────────────────────────────────────────────────
        ["Block.blocksList[?]"]                        = "BlockRegistry.Get(?)",
        ["Item.itemsList[?]"]                          = "ItemRegistry.Get(?)",

        // ── WorldGen constructors + generate calls ───────────────────────────
        ["new ky(?,?)"]                                = "new WorldGenMineable(?,?)",
        ["new kq()"]                                   = "new WorldGenTrees()",
        ["new aam()"]                                  = "new WorldGenLakes()",
        // .a(world, rng, x, y, z) = WorldGenerator.generate — 5-arg signature is unique
        [".a(?,?,?,?,?)"]                              = ".Generate(?,?,?,?,?)",

        // ── Java Random ──────────────────────────────────────────────────────
        ["rand.nextInt(?)"]                            = "rng.Next(?)",
        ["random.nextInt(?)"]                          = "rng.Next(?)",
        ["rand.nextFloat()"]                           = "(float)rng.NextDouble()",
        ["random.nextFloat()"]                         = "(float)rng.NextDouble()",
        ["rand.nextDouble()"]                          = "rng.NextDouble()",

        // ── Java System ──────────────────────────────────────────────────────
        ["System.out.println(?)"]                      = "Console.WriteLine(?)",
        ["System.err.println(?)"]                      = "Console.Error.WriteLine(?)",
    };

    /// <summary>
    /// Field access patterns: Java field name → C# property name.
    /// </summary>
    public static readonly Dictionary<string, string> FieldAccess = new()
    {
        ["block.blockIndexInTexture"]  = "block.TextureIndex",
        ["block.blockHardness"]        = "block.Hardness",
        ["block.blockResistance"]      = "block.BlastResistance",
        ["block.lightValue"]           = "block.LightEmission",
        ["block.lightOpacity"]         = "block.LightOpacity",
        ["block.blockMaterial"]        = "block.Material",

        // ── Item/Block ID field (.bM = itemID / blockID in 1.0) ──────────────
        [".bM"]                        = ".Id",

        // ── WorldGen context: Java variable name 'rand' → C# 'rng' ───────────
        [" rand,"]                     = " rng,",
        [" rand)"]                     = " rng)",
        ["(rand,"]                     = "(rng,",
        ["(rand)"]                     = "(rng)",

        ["player.posX"]                = "player.Position.X",
        ["player.posY"]                = "player.Position.Y",
        ["player.posZ"]                = "player.Position.Z",
        ["player.motionX"]             = "player.Velocity.X",
        ["player.motionY"]             = "player.Velocity.Y",
        ["player.motionZ"]             = "player.Velocity.Z",
        ["player.onGround"]            = "player.IsOnGround",
        ["player.health"]              = "player.Health",
        ["player.inventory"]           = "player.Inventory",

        ["stack.stackSize"]            = "stack.Count",
        ["stack.itemID"]               = "stack.ItemId",
        ["stack.getItemDamage()"]      = "stack.Damage",

        ["world.isRemote"]             = "world.IsClientSide",
        ["world.rand"]                 = "world.Random",
    };

    /// <summary>
    /// Applies all known substitutions to a single Java source line.
    /// Returns the translated line; unknown constructs are preserved with a TODO comment.
    /// </summary>
    public static string TranslateLine(string javaLine)
    {
        string line = javaLine;

        foreach (var (javaPattern, csPattern) in FieldAccess)
            line = line.Replace(javaPattern, csPattern);

        // Method calls use regex-style '?' wildcards — handled by Translator.
        // This method handles simple field substitutions only.

        return line;
    }
}
