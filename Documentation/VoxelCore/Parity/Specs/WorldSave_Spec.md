# WorldSave Spec
Sources: `e.java` (SaveHandler, 179 lines), `gy.java` (ChunkLoader, 246 lines),
         `si.java` (WorldInfo, 250 lines), `nh.java` (ISaveHandler interface, 18 lines),
         `d.java` (IChunkLoader interface, 11 lines), `vx.java` (NBT I/O, 124 lines),
         `um.java` (NBT base, 128 lines), `ik.java` (TAG_Compound, 167 lines),
         `yi.java` (TAG_List, 94 lines), `ahn.java` (ScheduledTick)
Type: File format + I/O algorithm reference

---

## 1. Overview

The save system has four layers:

| Layer | Class | Role |
|---|---|---|
| **ISaveHandler** (`nh`) | interface | World-level save: level.dat + per-dimension chunk loaders |
| **SaveHandler** (`e`) | concrete `nh` | Filesystem implementation; session lock; directory layout |
| **NullSaveHandler** (`bi`) | concrete `nh` | No-op stub for worlds that do not save |
| **IChunkLoader** (`d`) | interface | Per-chunk load/save |
| **ChunkLoader** (`gy`) | concrete `d` | Reads/writes per-chunk `.dat` files in a two-level directory tree |
| **WorldInfo** (`si`) | data class | All level.dat fields |
| **NBT I/O** (`vx`) | utility | GZip ↔ NBT stream; also plain (non-GZip) for some aux files |
| **NBT tags** (`um` subtypes) | value types | Typed tag hierarchy; TAG_Compound (`ik`), TAG_List (`yi`), primitives |

---

## 2. Class Identifiers

| Obfuscated | Human name | Notes |
|---|---|---|
| `nh` | `ISaveHandler` | interface |
| `e` | `SaveHandler` | implements `nh`; concrete filesystem handler |
| `bi` | `NullSaveHandler` | implements `nh`; all methods are no-ops / return null |
| `d` | `IChunkLoader` | interface |
| `gy` | `ChunkLoader` | implements `d`; one instance per dimension |
| `si` | `WorldInfo` | level.dat data container |
| `vx` | `NbtIo` | static serializer: GZip ↔ TAG_Compound |
| `um` | `NbtTag` | abstract base for all NBT tags |
| `ik` | `NbtCompound` (TAG_Compound) | type ID 10 |
| `yi` | `NbtList` (TAG_List) | type ID 9 |
| `ahn` | `ScheduledTick` | pending block tick; fields: `a`=x, `b`=y, `c`=z, `d`=blockId, `e`=absoluteTick |

---

## 3. Directory Layout

```
<worldName>/
  session.lock               8-byte big-endian long (timestamp); ensures single writer
  level.dat                  GZipped NBT; root = {Data: TAG_Compound}
  level.dat_old              previous level.dat, kept as backup after successful save
  level.dat_new              transient during atomic write; cleaned up on success
  players/                   per-player .dat files (multiplayer; not written in 1.0 SP)
  data/                      auxiliary named .dat files (maps, etc.)
  DIM-1/                     Nether chunks; same sub-tree layout as root
  DIM1/                      End chunks; same sub-tree layout as root
  <x_sub>/                   base-36 of (chunkX & 63); e.g. chunk X=-32 → x_sub = "w"
    <z_sub>/                 base-36 of (chunkZ & 63)
      c.<x>.<z>.dat          GZipped NBT for one chunk; x,z in signed base-36
```

`Integer.toString(n, 36)` in Java handles negative values with a leading `-`.
For example, chunk at X=−1 → base-36 = `"-1"`.

**Dimension routing** (from `e.a(k var1)` — `k` = WorldProvider):
- `aau` (NetherProvider) → `DIM-1/`
- `ol` (EndProvider) → `DIM1/`
- anything else (Overworld) → world root directory

---

## 4. Session Lock

Written once at world open:
```
<worldName>/session.lock  — DataOutputStream.writeLong(System.currentTimeMillis())
```

Verified on every save (`e.b()`):
```
read = DataInputStream.readLong(session.lock)
if read != this.e:            // this.e = timestamp written at open
    throw adl("The save is being accessed from another location, aborting")
```

`adl` = `SessionLockException` (unchecked). If the lock file's timestamp has changed,
another server instance has taken ownership; the save aborts.

---

## 5. NBT Wire Format

### Tag type IDs

| ID | Name | Obfuscated class | Payload |
|---|---|---|---|
| 0 | TAG_End | `hp` | none (terminates TAG_Compound) |
| 1 | TAG_Byte | `xq` | 1 byte signed |
| 2 | TAG_Short | `cg` | 2 bytes big-endian signed |
| 3 | TAG_Int | `hx` | 4 bytes big-endian signed |
| 4 | TAG_Long | `vw` | 8 bytes big-endian signed |
| 5 | TAG_Float | `vd` | 4 bytes IEEE 754 |
| 6 | TAG_Double | `fg` | 8 bytes IEEE 754 |
| 7 | TAG_Byte_Array | `ca` | int32 length, then length bytes |
| 8 | TAG_String | `yt` | Java `DataOutput.writeUTF` (2-byte length + modified UTF-8) |
| 9 | TAG_List | `yi` | see §5.2 |
| 10 | TAG_Compound | `ik` | see §5.1 |

### 5.1 TAG_Compound (`ik`) wire layout

```
[tag_byte]      type of next child (1 byte)
[name_utf]      name of child (DataOutput.writeUTF)
[payload]       child data (type-specific)
... (repeat for each child)
[0x00]          TAG_End terminator (no name, no payload)
```

Boolean values are stored as TAG_Byte: `true` → 1, `false` → 0.
`ik.a(String, boolean)` calls `a(name, (byte)(value ? 1 : 0))`.

### 5.2 TAG_List (`yi`) wire layout

```
[element_type]  byte — type ID of all elements
[count]         int32 — number of elements
[payload_0]     first element payload (no type byte, no name)
[payload_1]
...
```

Elements of a list have no type header and no name — only the raw payload.
If the list is empty, element_type is written as 1 (TAG_Byte) by convention.

### 5.3 File-level GZip framing

All `.dat` files (chunk files and level.dat) are GZip-compressed streams.
The root element is always a named TAG_Compound:
```
[0x0A]          TAG_Compound type byte
[name_utf]      root compound name (UTF)
[children...]
[0x00]          TAG_End
```

`vx.a(DataInput)` reads one tag with `um.b(DataInput)` and asserts it is a TAG_Compound.

`vx.b(ik, File)` writes without GZip (plain DataOutputStream) — used for auxiliary files.
`vx.a(ik, File)` writes with GZip via atomic rename (write to `<path>_tmp`, swap).

---

## 6. `level.dat` Format

Outer NBT root: unnamed TAG_Compound containing a single child:
```
"Data" → TAG_Compound (WorldInfo payload)
```

### Fields written by `si.a(ik, ik)` and `si.a(ik)`:

| NBT key | Type | Field | Notes |
|---|---|---|---|
| `"RandomSeed"` | long | `a` | World generation seed |
| `"GameType"` | int | `p` | 0=Survival, 1=Creative |
| `"MapFeatures"` | byte | `q` | true → generate structures (villages, etc.) |
| `"SpawnX"` | int | `b` | World spawn X |
| `"SpawnY"` | int | `c` | World spawn Y |
| `"SpawnZ"` | int | `d` | World spawn Z |
| `"Time"` | long | `e` | Game tick counter |
| `"SizeOnDisk"` | long | `g` | Rolling sum of chunk file sizes (bytes) |
| `"LastPlayed"` | long | — | `System.currentTimeMillis()` at save time; not stored as field |
| `"LevelName"` | string | `j` | World folder/display name |
| `"version"` | int | `k` | Save format version |
| `"rainTime"` | int | `m` | Ticks until next rain toggle |
| `"raining"` | byte | `l` | Currently raining |
| `"thunderTime"` | int | `o` | Ticks until next thunder toggle |
| `"thundering"` | byte | `n` | Currently thundering |
| `"hardcore"` | byte | `r` | Hardcore mode flag |
| `"Player"` | TAG_Compound | `h` | Player data (optional; only written if player list non-empty) |

When **saving with player list** (`si.a(List players)`):
- If `players.size() > 0`, takes the first player (`vi` = EntityPlayer), calls `vi.d(playerTag)` to populate a TAG_Compound, then writes it as `"Player"`.
- If no players, no `"Player"` key is written.

When **saving without player** (`si.a()`):
- Uses the cached `si.h` field (TAG_Compound read from disk at load time, if present).
- If `si.h == null`, no `"Player"` key is written.

### Atomic write sequence for `level.dat`:

```
1. Serialize to level.dat_new (GZip)
2. If level.dat_old exists → delete it
3. Rename level.dat → level.dat_old   (backup)
4. If level.dat still exists → delete it  (rename may have failed on some OSes)
5. Rename level.dat_new → level.dat
6. If level.dat_new still exists → delete it
```

---

## 7. `level.dat` Read

`e.c()` (implements `nh.c()`):
1. Try `level.dat` → `vx.a(FileInputStream)` → `ik.k("Data")` → `new si(dataTag)`.
2. If that fails for any reason, fall back to `level.dat_old`.
3. If both fail → return `null` (new world).

`si(ik)` constructor reads fields with typed getters:
- `ik.f(key)` → long
- `ik.e(key)` → int
- `ik.m(key)` → boolean (reads TAG_Byte, returns != 0)
- `ik.i(key)` → String
- `ik.k(key)` → TAG_Compound (for Player)
- `ik.b(key)` → hasKey check (used for MapFeatures and Player)

If `"MapFeatures"` key is absent → defaults to `true`.
If `"Player"` key is present → reads `si.h` and `si.i` (player dimension ID from `h.e("Dimension")`).

---

## 8. Chunk File Path

```
chunkX, chunkZ are signed integers.
subX = Integer.toString(chunkX & 63, 36)   // 0–63 → "0"–"1r"
subZ = Integer.toString(chunkZ & 63, 36)
fileName = "c." + Integer.toString(chunkX, 36) + "." + Integer.toString(chunkZ, 36) + ".dat"

path = <worldDir> / subX / subZ / fileName
```

If `b = false` (read-only mode), `gy.a(int, int)` returns null for any missing directory or file.
If `b = true` (read-write mode), missing directories are created with `mkdir()`.

---

## 9. Chunk NBT Format

Each chunk `.dat` file contains one GZipped NBT root compound (unnamed), whose sole child is:

```
"Level" → TAG_Compound
```

### Fields written by `gy.a(zx chunk, ry world, ik levelTag)`:

| NBT key | Type | Source | Notes |
|---|---|---|---|
| `"xPos"` | int | `chunk.l` | Chunk X coordinate |
| `"zPos"` | int | `chunk.m` | Chunk Z coordinate |
| `"LastUpdate"` | long | `world.u()` | World tick at save time |
| `"Blocks"` | byte[] | `chunk.b` | 32 768 bytes; index = `(x<<11)|(z<<7)|y` |
| `"Data"` | byte[] | `chunk.g.a` | Nibble array, 16 384 bytes; block metadata |
| `"SkyLight"` | byte[] | `chunk.h.a` | Nibble array, 16 384 bytes |
| `"BlockLight"` | byte[] | `chunk.i.a` | Nibble array, 16 384 bytes |
| `"HeightMap"` | byte[] | `chunk.j` | 256 bytes; index = `z<<4|x` |
| `"TerrainPopulated"` | byte | `chunk.p` | 0/1; whether decoration pass has run |
| `"Entities"` | TAG_List(TAG_Compound) | `chunk.o[layer]` | All entities in chunk via `ia.c(tag)` |
| `"TileEntities"` | TAG_List(TAG_Compound) | `chunk.n.values()` | All TileEntities via `bq.a(tag)` |
| `"TileTicks"` | TAG_List(TAG_Compound) | world pending ticks | Optional; see §9.1 |

`chunk.s` (hasEntities flag) is set to `false` at save start, then set to `true` if any
entity's `ia.c(tag)` returns true (entity consented to being saved).

### 9.1 TileTicks (Pending Block Ticks)

Retrieved via `world.a(chunk, false)` — returns a `List<ahn>` or null.
Each `ahn` tick is written as a TAG_Compound:

| Key | Type | Value |
|---|---|---|
| `"i"` | int | Block ID (`ahn.d`) |
| `"x"` | int | `ahn.a` |
| `"y"` | int | `ahn.b` |
| `"z"` | int | `ahn.c` |
| `"t"` | int | `ahn.e - world.u()` — ticks remaining |

If `world.a(chunk, false)` returns null, no `"TileTicks"` key is written.

---

## 10. Chunk Load Algorithm (`gy.a(ry, int, int)`)

```
1. Compute file path.
2. If file does not exist → return null (chunk not yet generated).
3. Open FileInputStream → vx.a(stream) → root TAG_Compound.
4. Assert root.hasKey("Level") — else log warning, return null.
5. Assert root["Level"].hasKey("Blocks") — else log warning, return null.
6. Call gy.a(world, root["Level"]) → build Chunk object (see §10.1).
7. If chunk.xPos / chunk.zPos do not match requested coords:
     Log relocation warning.
     Fix root["xPos"] and root["zPos"] in the compound.
     Rebuild chunk from corrected compound.
8. Call chunk.i() — marks chunk as loaded / fires load event.
9. Return chunk.
```

### 10.1 Chunk deserialization (`static gy.a(ry, ik)`)

```
chunk.xPos  = level["xPos"]
chunk.zPos  = level["zPos"]
chunk.b     = level.j("Blocks")         // raw block ID bytes
chunk.g     = new up(level.j("Data"), world.a)      // Data nibble array
chunk.h     = new up(level.j("SkyLight"), world.a)  // SkyLight nibble array
chunk.i     = new up(level.j("BlockLight"), world.a) // BlockLight nibble array
chunk.j     = level.j("HeightMap")
chunk.p     = level.m("TerrainPopulated")

if !chunk.g.a():     // Data nibble array invalid / empty
    chunk.g = new up(chunk.b.length, world.a)     // reset to zeroes

if chunk.j == null OR !chunk.h.a():     // HeightMap or SkyLight invalid
    chunk.j = new byte[256]
    chunk.h = new up(chunk.b.length, world.a)
    chunk.c()                           // recalculate sky light (GenerateSkylightMap)

if !chunk.i.a():     // BlockLight invalid
    chunk.i = new up(chunk.b.length, world.a)
    chunk.a()                           // recalculate block light
```

`up` = NibbleArray. `up.a()` returns whether the array contains any valid data
(likely: checks if any byte is non-zero, i.e. not a blank array).
`world.a` is the world's array size parameter (16 384 for a full chunk half-array).

After arrays, entities are deserialized:
```
for each TAG_Compound in level["Entities"]:
    ia entity = afw.a(entityTag, world)   // EntityList.createEntityFromNBT
    if entity != null:
        chunk.addEntity(entity)
        chunk.s = true

for each TAG_Compound in level["TileEntities"]:
    bq te = bq.c(teTag)                  // TileEntity.createAndLoadEntity
    if te != null:
        chunk.addTileEntity(te)

if level.hasKey("TileTicks"):
    for each TAG_Compound tick in level["TileTicks"]:
        world.e(tick["x"], tick["y"], tick["z"], tick["i"], tick["t"])
        // schedules the tick back into the world queue with remaining time
```

---

## 11. Chunk Save Algorithm (`gy.a(ry, zx)`)

```
1. world.s()  — verify session lock (throws if stolen).
2. Compute target file path.
3. If target exists:
     si = world.z()                 // get WorldInfo
     si.b(si.g() - targetFile.length())   // subtract old file size from SizeOnDisk
4. Write to tmp_chunk.dat (in worldDir root):
     root = new ik("")
     level = new ik("")
     root.a("Level", level)
     gy.a(chunk, world, level)      // populate all fields (see §9)
     vx.a(root, FileOutputStream("tmp_chunk.dat"))
5. If target exists → delete it.
6. tmp_chunk.dat.renameTo(target).
7. si = world.z()
   si.b(si.g() + target.length())  // add new file size to SizeOnDisk
```

Note: chunk files use a single shared `tmp_chunk.dat` in the world root, not per-chunk temp files. This is not thread-safe for concurrent saves — by design (1.0 is single-threaded).

---

## 12. IChunkLoader Interface (`d`)

| Method | Signature | Meaning |
|---|---|---|
| `a` | `zx a(ry world, int x, int z)` | Load chunk; null if not saved yet |
| `a` | `void a(ry world, zx chunk)` | Save chunk |
| `b` | `void b(ry world, zx chunk)` | Post-save / secondary flush (empty in `gy`) |
| `a` | `void a()` | Flush / close (empty in `gy`) |
| `b` | `void b()` | Secondary close (empty in `gy`) |

---

## 13. ISaveHandler Interface (`nh`)

| Method | Signature | Meaning |
|---|---|---|
| `c` | `si c()` | Load WorldInfo from level.dat; null if new world |
| `b` | `void b()` | Verify session lock |
| `a` | `d a(k provider)` | Get IChunkLoader for the given WorldProvider (dimension routing) |
| `a` | `void a(si, List<vi>)` | Save level.dat with player data from player list |
| `a` | `void a(si)` | Save level.dat without player (uses cached si.h if any) |
| `a` | `File a(String name)` | Get file for named auxiliary data (e.g. `name+".dat"` in `data/`) |
| `d` | `String d()` | Returns world folder name |

---

## 14. Data Accessor Reference (`si` — WorldInfo)

| Method | Field | Type | Meaning |
|---|---|---|---|
| `b()` | `a` | long | Random seed |
| `c()` | `b` | int | Spawn X |
| `d()` | `c` | int | Spawn Y |
| `e()` | `d` | int | Spawn Z |
| `f()` | `e` | long | Game time (ticks) |
| `g()` | `g` | long | SizeOnDisk (bytes) |
| `h()` | `h` | ik | Cached player tag |
| `i()` | `i` | int | Player dimension (from cached player tag) |
| `j()` | `j` | String | Level name |
| `k()` | `k` | int | Save format version |
| `l()` | `f` | long | Last played timestamp (from disk; not updated in si) |
| `m()` | `n` | boolean | Thundering |
| `n()` | `o` | int | Thunder time |
| `o()` | `l` | boolean | Raining |
| `p()` | `m` | int | Rain time |
| `q()` | `p` | int | Game type (0=survival, 1=creative) |
| `r()` | `q` | boolean | Map features (structures) enabled |
| `s()` | `r` | boolean | Hardcore mode |

Setters: `a(int)` = setSpawnX, `b(int)` = setSpawnY, `c(int)` = setSpawnZ,
`a(long)` = setTime, `b(long)` = setSizeOnDisk, `a(ik)` = setPlayerTag,
`a(int,int,int)` = setSpawn(x,y,z), `a(String)` = setLevelName, `d(int)` = setVersion,
`a(boolean)` = setThundering, `e(int)` = setThunderTime, `b(boolean)` = setRaining,
`f(int)` = setRainTime.

---

## 15. Quirks to Preserve

- **SizeOnDisk is a rolling counter**: `SaveHandler.a(world, chunk)` subtracts the old file
  size before overwriting and adds the new size after. If a chunk file was deleted externally,
  the counter drifts — no periodic recalculation.
- **tmp_chunk.dat races**: Single temp file in world root; not safe for concurrent chunk saves.
  In 1.0 this is fine (single-threaded), but the constraint must be respected.
- **Chunk coord mismatch recovery**: If a chunk file's stored xPos/zPos don't match the
  requested coords, the coords are patched in-memory and the chunk is re-deserialized.
  The fixed data is **not** immediately re-saved to disk — it gets saved on the next dirty flush.
- **NBT boolean = TAG_Byte**: Java `boolean` fields are always read with `ik.m(key)` (`c(key) != 0`)
  and written with `ik.a(key, boolean)` which calls `a(key, (byte)(v ? 1 : 0))`. No TAG_Byte vs
  TAG_Boolean distinction in the format.
- **Entities opt-in**: Entity serialization calls `ia.c(tag)` which returns `true` if the entity
  consented to be saved. If `c()` returns false, the entity is not included and `chunk.s` stays
  false (no entities flag) — preventing needless resaving of entity-less chunks.
- **Level backup chain**: level.dat_old is a one-generation backup. The write sequence ensures
  that either level.dat or level.dat_old is always intact, even if the process is killed mid-save.

---

*Spec written by Analyst AI from `e.java`, `gy.java`, `si.java`, `nh.java`, `d.java`,
`vx.java`, `um.java`, `ik.java`, `yi.java`, `ahn.java`. No C# implementation consulted.*
*(Addresses Coder request [STATUS:REQUIRED] — WorldSave)*
