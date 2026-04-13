# Vanilla API Translation Table

Maps Java method calls commonly used by mods to their SpectraSharp C# equivalents.
The Mod Analyst uses this table when writing injection descriptions in Mod Specs.

> All Java names are the MCP-style human-readable names as found after deobfuscation.
> Obfuscated equivalents are in `Documentation/VoxelCore/Parity/Mappings/classes.md`.

## World / Block Access

| Java (Mod Code) | C# (SpectraSharp) | Notes |
|---|---|---|
| `world.getBlockId(x, y, z)` | `World.GetBlockId(x, y, z)` | Returns `int` block ID (0–255) |
| `world.setBlock(x, y, z, id)` | `World.SetBlock(x, y, z, id)` | Queues block update |
| `world.setBlockWithNotify(x, y, z, id)` | `World.SetBlockNotify(x, y, z, id)` | Also triggers neighbour updates |
| `world.getBlockMetadata(x, y, z)` | `World.GetBlockMeta(x, y, z)` | Returns `byte` metadata |
| `world.setBlockMetadataWithNotify(x, y, z, meta)` | `World.SetBlockMeta(x, y, z, meta)` | |
| `world.isAirBlock(x, y, z)` | `World.IsAir(x, y, z)` | `GetBlockId == 0` |
| `world.markBlocksDirty(x1,y1,z1,x2,y2,z2)` | `World.MarkDirty(bounds)` | Triggers re-render |
| `world.scheduleBlockUpdate(x,y,z,id,delay)` | `World.ScheduleTick(x,y,z,id,delay)` | `delay` in ticks (1 tick = 0.05 s) |

## Entity / Player

| Java (Mod Code) | C# (SpectraSharp) | Notes |
|---|---|---|
| `player.inventory.addItemStackToInventory(stack)` | `Player.Inventory.TryAdd(stack)` | Returns `bool` |
| `player.sendChatMessage(text)` | `Player.SendChat(text)` | |
| `player.hurtPlayer(dmg, "cause")` | `Player.Hurt(dmg, cause)` | `dmg` in half-hearts |
| `player.posX / posY / posZ` | `Player.Position.X/Y/Z` | `double` |
| `player.motionX / motionY / motionZ` | `Player.Velocity.X/Y/Z` | `double` per tick |
| `player.onGround` | `Player.IsOnGround` | `bool` |

## Item / ItemStack

| Java (Mod Code) | C# (SpectraSharp) | Notes |
|---|---|---|
| `new ItemStack(Item.someItem, count, damage)` | `new ItemStack(itemId, count, damage)` | |
| `stack.getItem()` | `stack.Item` | Returns `ItemBase` |
| `stack.stackSize` | `stack.Count` | `int` |
| `stack.getItemDamage()` | `stack.Damage` | `int` |
| `Item.itemsList[id]` | `ItemRegistry.Get(id)` | Returns `ItemBase?` |

## Block Registration

| Java (Mod Code) | C# (SpectraSharp) | Notes |
|---|---|---|
| `Block.blocksList[id] = new MyBlock(id)` | Derive from `BlockBase`, set `BlockId` | Auto-registered via `BridgeRegistry` |
| `block.setHardness(f)` | `BlockBase.Hardness = f` | Float, mining time multiplier |
| `block.setResistance(f)` | `BlockBase.BlastResistance = f` | Explosion resistance |
| `block.setLightValue(f)` | `BlockBase.LightEmission = f` | 0.0–1.0, maps to 0–15 |
| `block.setLightOpacity(i)` | `BlockBase.LightOpacity = i` | 0–255 |
| `block.setBlockName(name)` | `BlockBase.UnlocalizedName = name` | |

## Rendering / Texture

| Java (Mod Code) | C# (SpectraSharp) | Notes |
|---|---|---|
| `block.blockIndexInTexture` | `BlockBase.TextureIndex` | Row-major index into terrain.png (16×16 grid) |
| `ModLoader.addOverride("/terrain.png", "/mymod/mytex.png", slot)` | `TerrainAtlas.RegisterOverride(slot, texturePath)` | `slot` = terrain.png index |
| `ModLoader.addOverride("/gui/items.png", "/mymod/myitem.png", slot)` | `ItemAtlas.RegisterOverride(slot, texturePath)` | |
| `RenderBlocks.renderBlockByRenderType(block, x, y, z)` | `RenderBlocks.Render(block, x, y, z)` | Called by chunk renderer |

## Sound

| Java (Mod Code) | C# (SpectraSharp) | Notes |
|---|---|---|
| `world.playSoundAtEntity(entity, "step.stone", vol, pitch)` | `Audio.PlayAt(entity.Position, "step.stone", vol, pitch)` | |
| `world.playSoundEffect(x,y,z, name, vol, pitch)` | `Audio.PlayAt(x, y, z, name, vol, pitch)` | |

## ModLoader Hooks (Risugami API — 1.0 era)

These are methods mods register via `ModLoader` in 1.0. Map each to a HarmonyLib patch.

| Java Hook | HarmonyLib Patch Point | C# Equivalent |
|---|---|---|
| `mod_X.generateSurface(world, rand, chunk_x, chunk_z)` | Postfix on `WorldGenerator.PopulateChunk` | `IWorldGenHook.OnPopulate(...)` |
| `mod_X.onTickInGame(mc)` | Postfix on `Engine.FixedUpdate` | `ITickHook.OnTick(world)` |
| `mod_X.onItemPickup(player, stack)` | Postfix on `Player.PickupItem` | `IPickupHook.OnPickup(...)` |
| `mod_X.onBlockRemoved(world, x, y, z, id, meta)` | Postfix on `World.SetBlock` | `IBlockHook.OnRemoved(...)` |
| `mod_X.addRecipes(craftManager)` | Prefix on `CraftingManager.Init` | `IRecipeHook.RegisterRecipes(...)` |
