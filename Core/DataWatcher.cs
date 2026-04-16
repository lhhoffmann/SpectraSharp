namespace SpectraEngine.Core;

/// <summary>
/// Replica of <c>cr</c> (DataWatcher) — per-entity typed data dictionary that synchronises
/// a small set of values from server to client. Entries are identified by integer IDs (0–31)
/// and may hold one of 7 supported types.
///
/// Entity base class registers two entries in its constructor:
///   index 0 = entity flags byte, index 1 = air-supply short.
/// Subclasses add their own entries via <see cref="Register"/>.
///
/// Quirks preserved (see spec §8):
///   1. Max entry ID is 31 (5 bits). ID > 31 throws.
///   2. Client-side <c>ApplyChanges</c> does NOT set the dirty flag.
///   3. <c>GetAllDirty</c> (network read) returns null (not an empty list) if no entries.
///   4. Equality check in UpdateObject uses <c>object.Equals</c>.
///
/// Wire format (spec §6):
///   byte header = (typeId &lt;&lt; 5 | entryId) &amp; 0xFF, followed by value bytes.
///   Terminator: 0x7F after last entry.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/DataWatcher_Spec.md
/// </summary>
public sealed class DataWatcher
{
    // ── Static type registry (spec §3) — shared by all instances ─────────────

    private static readonly Dictionary<Type, int> TypeRegistry = new()
    {
        [typeof(byte)]      = 0,
        [typeof(short)]     = 1,
        [typeof(int)]       = 2,
        [typeof(float)]     = 3,
        [typeof(string)]    = 4,
        [typeof(ItemStack)] = 5,
        // ChunkCoordinates (dh) = typeId 6 — pending dh spec
    };

    // ── Instance fields (spec §2) ─────────────────────────────────────────────

    private readonly Dictionary<int, WatchableObject> _entries = new(); // obf: b
    private          bool _isDirty;                                      // obf: c

    // ── Register (spec §5) ───────────────────────────────────────────────────

    /// <summary>
    /// Registers a new DataWatcher entry. Spec: <c>a(int id, Object initialValue)</c>.
    /// Throws if <paramref name="id"/> &gt; 31, type unknown, or id already registered (quirk 1).
    /// </summary>
    public void Register(int id, object initialValue)
    {
        if (id > 31)
            throw new ArgumentOutOfRangeException(nameof(id), "DataWatcher id must be 0–31 (quirk 1)");
        if (_entries.ContainsKey(id))
            throw new InvalidOperationException($"DataWatcher id {id} is already registered");
        if (!TypeRegistry.TryGetValue(initialValue.GetType(), out int typeId))
            throw new ArgumentException($"Unsupported DataWatcher type: {initialValue.GetType()}");
        _entries[id] = new WatchableObject(typeId, id, initialValue);
    }

    // ── Typed getters (spec §5) ──────────────────────────────────────────────

    /// <summary>obf: <c>a(int id)</c> — getWatchableObjectByte.</summary>
    public byte GetByte(int id) => (byte)_entries[id].Value;

    /// <summary>obf: <c>b(int id)</c> — getWatchableObjectShort.</summary>
    public short GetShort(int id) => (short)_entries[id].Value;

    /// <summary>obf: <c>c(int id)</c> — getWatchableObjectInt.</summary>
    public int GetInt(int id) => (int)_entries[id].Value;

    /// <summary>obf: <c>d(int id)</c> — getWatchableObjectString.</summary>
    public string GetString(int id) => (string)_entries[id].Value;

    // ── Update (spec §5) ─────────────────────────────────────────────────────

    /// <summary>
    /// Server-side write. Only marks dirty if the value actually changed (quirk 4: uses Equals).
    /// Spec: <c>b(int id, Object value)</c>.
    /// </summary>
    public void UpdateObject(int id, object value)
    {
        var entry = _entries[id];
        if (!value.Equals(entry.Value))
        {
            entry.Value   = value;
            entry.IsDirty = true;
            _isDirty      = true;
        }
    }

    /// <summary>
    /// Client-side apply. Copies values without setting dirty flag (quirk 2).
    /// Spec: <c>a(List&lt;afh&gt; updates)</c>.
    /// </summary>
    public void ApplyChanges(IEnumerable<WatchableObject> updates)
    {
        foreach (var update in updates)
            if (_entries.TryGetValue(update.EntryId, out var existing))
                existing.Value = update.Value; // no dirty flag (quirk 2)
    }

    // ── Dirty flag helpers ────────────────────────────────────────────────────

    /// <summary>True if any entry was updated since the last <see cref="ClearDirty"/>.</summary>
    public bool IsDirty => _isDirty;

    /// <summary>Clears the dirty flag on all entries and on the watcher itself.</summary>
    public void ClearDirty()
    {
        _isDirty = false;
        foreach (var e in _entries.Values) e.IsDirty = false;
    }

    /// <summary>
    /// Returns all dirty entries, or null if none (quirk 3).
    /// Spec: used by network serialisation path.
    /// </summary>
    public List<WatchableObject>? GetAllDirty()
    {
        List<WatchableObject>? result = null;
        foreach (var e in _entries.Values)
        {
            if (e.IsDirty)
            {
                result ??= new List<WatchableObject>();
                result.Add(e);
            }
        }
        return result; // may be null — callers must null-check (quirk 3)
    }

    /// <summary>Returns all entries (for initial sync).</summary>
    public IEnumerable<WatchableObject> GetAll() => _entries.Values;

    // ── Wire constants (spec §6) ──────────────────────────────────────────────

    /// <summary>Wire terminator byte written after the last DataWatcher entry.</summary>
    public const byte Terminator = 0x7F;

    /// <summary>Encodes the wire header byte for a given type and entry ID.</summary>
    public static byte EncodeHeader(int typeId, int entryId) => (byte)((typeId << 5 | entryId) & 0xFF);

    // ── WatchableObject — afh (spec §4) ──────────────────────────────────────

    /// <summary>
    /// Internal container for a single DataWatcher entry. Replica of <c>afh</c>.
    /// </summary>
    public sealed class WatchableObject
    {
        public int    TypeId;   // obf: c()
        public int    EntryId;  // obf: a()
        public object Value;    // obf: b()
        public bool   IsDirty;  // obf: a(bool)

        public WatchableObject(int typeId, int entryId, object initialValue)
        {
            TypeId  = typeId;
            EntryId = entryId;
            Value   = initialValue;
        }
    }
}
