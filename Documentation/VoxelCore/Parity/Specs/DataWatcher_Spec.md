# DataWatcher Spec
Source class: `cr.java`
Type: `class`
Superclass: none (implicit `java.lang.Object`)

---

## 1. Purpose

`DataWatcher` (`cr`) is a per-entity dictionary that synchronises a small set of typed values
from server to client. Each entry has an integer ID (0–31) and a value of one of 7 supported
types. Entity base class (`ia`) registers two entries in its constructor (index 0 = flags byte,
index 1 = air-supply short). Subclasses add their own entries via `a(int id, Object)`.

---

## 2. Fields

| Field | Type | Semantics |
|---|---|---|
| `a` (static, private) | `HashMap<Class, Integer>` | Type registry: maps Java class → typeId (0–6). Shared by all instances. |
| `b` (private) | `HashMap<Integer, afh>` | Entry map: entryId → WatchableObject (`afh`) |
| `c` (private) | `boolean` | isDirty — set `true` when any entry changes; used for network sync |

---

## 3. Type Registry

Populated in static initializer:

| typeId | Java class | Wire size |
|---|---|---|
| 0 | `Byte` | 1 byte |
| 1 | `Short` | 2 bytes |
| 2 | `Integer` | 4 bytes |
| 3 | `Float` | 4 bytes |
| 4 | `String` | length-prefixed UTF-8 (via `gt.a`) |
| 5 | `dk` (ItemStack) | 5 bytes (itemId short + stackSize byte + damage short) |
| 6 | `dh` (ChunkCoordinates) | 12 bytes (x int + y int + z int) |

---

## 4. WatchableObject — `afh`

Internal container for a single DataWatcher entry. Has:
- `c()` → int: typeId
- `a()` → int: entryId
- `b()` → Object: current value
- `a(Object)`: setValue
- `a(boolean)`: setDirty

Constructor: `new afh(typeId, entryId, initialValue)`

---

## 5. Methods

### register — `a(int id, Object initialValue)`

Registers a new entry. Throws on:
- Unknown type (class not in static registry)
- `id > 31`
- Duplicate id

Stored as `new afh(typeId, id, initialValue)` in map `b`.

### Typed getters

| Method | Signature | Returns |
|---|---|---|
| getWatchableObjectByte | `a(int id)` → `byte` | `(Byte) b.get(id).b()` |
| getWatchableObjectShort | `b(int id)` → `short` | `(Short) b.get(id).b()` |
| getWatchableObjectInt | `c(int id)` → `int` | `(Integer) b.get(id).b()` |
| getWatchableObjectString | `d(int id)` → `String` | `(String) b.get(id).b()` |

No dedicated getter for Float, ItemStack, or ChunkCoordinates in the base class; those are
read via casting the Object returned by `afh.b()` directly in subclass code.

### updateObject — `b(int id, Object value)`

```java
afh entry = (afh)b.get(id);
if (!value.equals(entry.b())) {
    entry.a(value);   // setValue
    entry.a(true);    // setDirty
    this.c = true;    // mark whole watcher dirty
}
```

Only marks dirty if value actually changed (uses `equals`).

### applyChanges (from server) — `a(List<afh> updates)`

```java
for (afh update : updates) {
    afh existing = (afh)b.get(update.a());  // get by id
    if (existing != null) existing.a(update.b());  // setValue (no dirty mark)
}
```

Used on the client side to apply received network updates.

---

## 6. Network Serialisation

### Wire format (per entry)

```
byte header = (typeId << 5 | entryId) & 0xFF
```

Type bits occupy the upper 3 bits, ID the lower 5 bits.

After the header, value bytes follow per the type table in §3.

### Terminator

After the last entry: `writeByte(127)` (= `0x7F`). Reading stops at this sentinel.

### Static write — `a(List<afh>, DataOutputStream)`

Writes a list of entries (typically only dirty ones) then the `0x7F` terminator.

### Instance write — `a(DataOutputStream)`

Writes **all** entries from `b` map then terminator.

### Static read — `a(DataInputStream)` → `List<afh>`

Reads entries until `0x7F`. Returns `null` if no entries were present (never an empty list).

---

## 7. Entity Usage

Entity `ia` constructor registers:
```java
ag.a(0, (byte)0);     // index 0 = entity flags (bit field)
ag.a(1, (short)300);  // index 1 = air supply
```

Standard Entity DataWatcher indices:

| Index | Type | Semantics |
|---|---|---|
| 0 | byte | Entity flags bit field — see Entity_Spec.md §6 |
| 1 | short | Air supply (0 = drowning, 300 = full) |

Subclasses register additional indices. IDs must be unique per entity instance;
subclass IDs start from 2 (or higher if needed).

---

## 8. Known Quirks / Bugs to Preserve

| # | Quirk |
|---|---|
| 1 | Max entry ID is 31 (5 bits). Registering id > 31 throws `IllegalArgumentException`. |
| 2 | `a(List)` (client-side apply) does NOT set the dirty flag — it copies values silently. Only `b(int, Object)` (server-side update) marks dirty. |
| 3 | The static read `a(DataInputStream)` returns `null` (not an empty list) if no entries precede the `0x7F` terminator. Callers must null-check before iterating. |
| 4 | Equality check in `b()` uses `Object.equals()` — for `Float` and `Short` this is value equality; for `dk` (ItemStack) this uses ItemStack's `equals()` which may not be a deep comparison. |

---

## 9. Open Questions

1. **`afh` (WatchableObject)** — simple holder class. Confirmed fields and methods from usage.
   Full `afh.java` not read; assume straightforward struct-like class.

2. **`gt`** — string serialization helper. `gt.a(String, DataOutputStream)` and
   `gt.a(DataInputStream, maxLength)` = length-prefixed UTF-8. Spec pending if needed.

---

*Spec written by Analyst AI from `cr.java` (167 lines). No C# implementation consulted.*
