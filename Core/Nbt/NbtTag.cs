namespace SpectraSharp.Core.Nbt;

// ── Abstract base ──────────────────────────────────────────────────────────────

/// <summary>
/// Abstract base for all NBT tags. Replica of <c>um</c>.
/// Type IDs match the Java NBT specification exactly (see spec §5).
/// </summary>
public abstract class NbtTag
{
    public abstract byte TypeId { get; }
    /// <summary>Wire-encodes the payload only (no type byte, no name).</summary>
    internal abstract void WritePayload(NbtBinaryWriter w);
}

// ── TAG_End (0) ───────────────────────────────────────────────────────────────

/// <summary>Sentinel that terminates a TAG_Compound. obf: <c>hp</c>.</summary>
internal sealed class NbtEnd : NbtTag
{
    internal static readonly NbtEnd Instance = new();
    public override byte TypeId => 0;
    internal override void WritePayload(NbtBinaryWriter _) { }
}

// ── TAG_Byte (1) ──────────────────────────────────────────────────────────────

/// <summary>Signed 8-bit integer. obf: <c>xq</c>.</summary>
public sealed class NbtByte : NbtTag
{
    public byte Value;
    public NbtByte(byte value) { Value = value; }
    public override byte TypeId => 1;
    internal override void WritePayload(NbtBinaryWriter w) => w.WriteByte(Value);
}

// ── TAG_Short (2) ─────────────────────────────────────────────────────────────

/// <summary>Signed 16-bit integer. obf: <c>cg</c>.</summary>
public sealed class NbtShort : NbtTag
{
    public short Value;
    public NbtShort(short value) { Value = value; }
    public override byte TypeId => 2;
    internal override void WritePayload(NbtBinaryWriter w) => w.WriteShort(Value);
}

// ── TAG_Int (3) ───────────────────────────────────────────────────────────────

/// <summary>Signed 32-bit integer. obf: <c>hx</c>.</summary>
public sealed class NbtInt : NbtTag
{
    public int Value;
    public NbtInt(int value) { Value = value; }
    public override byte TypeId => 3;
    internal override void WritePayload(NbtBinaryWriter w) => w.WriteInt(Value);
}

// ── TAG_Long (4) ──────────────────────────────────────────────────────────────

/// <summary>Signed 64-bit integer. obf: <c>vw</c>.</summary>
public sealed class NbtLong : NbtTag
{
    public long Value;
    public NbtLong(long value) { Value = value; }
    public override byte TypeId => 4;
    internal override void WritePayload(NbtBinaryWriter w) => w.WriteLong(Value);
}

// ── TAG_Float (5) ─────────────────────────────────────────────────────────────

/// <summary>IEEE 754 32-bit float. obf: <c>vd</c>.</summary>
public sealed class NbtFloat : NbtTag
{
    public float Value;
    public NbtFloat(float value) { Value = value; }
    public override byte TypeId => 5;
    internal override void WritePayload(NbtBinaryWriter w) => w.WriteInt(BitConverter.SingleToInt32Bits(Value));
}

// ── TAG_Double (6) ────────────────────────────────────────────────────────────

/// <summary>IEEE 754 64-bit double. obf: <c>fg</c>.</summary>
public sealed class NbtDouble : NbtTag
{
    public double Value;
    public NbtDouble(double value) { Value = value; }
    public override byte TypeId => 6;
    internal override void WritePayload(NbtBinaryWriter w) => w.WriteLong(BitConverter.DoubleToInt64Bits(Value));
}

// ── TAG_Byte_Array (7) ────────────────────────────────────────────────────────

/// <summary>Length-prefixed byte array. obf: <c>ca</c>.</summary>
public sealed class NbtByteArray : NbtTag
{
    public byte[] Value;
    public NbtByteArray(byte[] value) { Value = value; }
    public override byte TypeId => 7;
    internal override void WritePayload(NbtBinaryWriter w) { w.WriteInt(Value.Length); w.WriteBytes(Value); }
}

// ── TAG_String (8) ────────────────────────────────────────────────────────────

/// <summary>Java DataOutput.writeUTF string (2-byte length + UTF-8 bytes). obf: <c>yt</c>.</summary>
public sealed class NbtString : NbtTag
{
    public string Value;
    public NbtString(string value) { Value = value; }
    public override byte TypeId => 8;
    internal override void WritePayload(NbtBinaryWriter w) => w.WriteUtf(Value);
}

// ── TAG_List (9) ──────────────────────────────────────────────────────────────

/// <summary>
/// Homogeneous list of NBT tags. No type or name per element.
/// If empty, element type is written as 1 (TAG_Byte) per spec §5.2. obf: <c>yi</c>.
/// </summary>
public sealed class NbtList : NbtTag
{
    private readonly List<NbtTag> _items = [];

    /// <summary>Expected element type; 1 (TAG_Byte) if list is empty.</summary>
    public byte ElementTypeId { get; private set; } = 1;

    public override byte TypeId => 9;

    public int Count => _items.Count;
    public NbtTag this[int i] => _items[i];

    public void Add(NbtTag tag)
    {
        if (_items.Count == 0) ElementTypeId = tag.TypeId;
        _items.Add(tag);
    }

    public IReadOnlyList<NbtTag> Items => _items;

    internal override void WritePayload(NbtBinaryWriter w)
    {
        w.WriteByte(ElementTypeId);
        w.WriteInt(_items.Count);
        foreach (var tag in _items) tag.WritePayload(w);
    }
}

// ── TAG_Compound (10) ────────────────────────────────────────────────────────

/// <summary>
/// Named collection of heterogeneous NBT tags. Terminated by TAG_End. obf: <c>ik</c>.
/// All typed Put/Get helpers match the spec's <c>ik.a/e/f/m/i/k/j</c> signatures.
/// Boolean values are TAG_Byte under the hood (spec §5.1 quirk).
/// </summary>
public sealed class NbtCompound : NbtTag
{
    private readonly Dictionary<string, NbtTag> _map = new(StringComparer.Ordinal);

    public override byte TypeId => 10;

    // ── Containment ───────────────────────────────────────────────────────────

    /// <summary>obf: <c>ik.b(key)</c> — returns true if key is present.</summary>
    public bool HasKey(string key) => _map.ContainsKey(key);

    public IReadOnlyDictionary<string, NbtTag> All => _map;

    // ── Put helpers ───────────────────────────────────────────────────────────

    public void Put(string key, NbtTag value)           => _map[key] = value;
    public void PutByte(string key, byte value)         => _map[key] = new NbtByte(value);
    public void PutShort(string key, short value)       => _map[key] = new NbtShort(value);
    public void PutInt(string key, int value)           => _map[key] = new NbtInt(value);
    public void PutLong(string key, long value)         => _map[key] = new NbtLong(value);
    public void PutFloat(string key, float value)       => _map[key] = new NbtFloat(value);
    public void PutDouble(string key, double value)     => _map[key] = new NbtDouble(value);
    public void PutString(string key, string value)     => _map[key] = new NbtString(value);
    public void PutByteArray(string key, byte[] value)  => _map[key] = new NbtByteArray(value);
    public void PutList(string key, NbtList value)      => _map[key] = value;
    public void PutCompound(string key, NbtCompound value) => _map[key] = value;

    /// <summary>Boolean stored as TAG_Byte (1 = true, 0 = false). Spec §5.1.</summary>
    public void PutBoolean(string key, bool value) => PutByte(key, (byte)(value ? 1 : 0));

    // ── Get helpers (return default for missing keys) ─────────────────────────

    /// <summary>obf: <c>ik.e(key)</c> — get int, 0 if missing.</summary>
    public int GetInt(string key)
        => _map.TryGetValue(key, out var t) && t is NbtInt i ? i.Value : 0;

    /// <summary>obf: <c>ik.f(key)</c> — get long, 0 if missing.</summary>
    public long GetLong(string key)
        => _map.TryGetValue(key, out var t) && t is NbtLong l ? l.Value : 0L;

    /// <summary>obf: <c>ik.c(key)</c> — get byte value, 0 if missing.</summary>
    public byte GetByte(string key)
        => _map.TryGetValue(key, out var t) && t is NbtByte b ? b.Value : (byte)0;

    public short GetShort(string key)
        => _map.TryGetValue(key, out var t) && t is NbtShort s ? s.Value : (short)0;

    public float GetFloat(string key)
        => _map.TryGetValue(key, out var t) && t is NbtFloat f ? f.Value : 0f;

    public double GetDouble(string key)
        => _map.TryGetValue(key, out var t) && t is NbtDouble d ? d.Value : 0.0;

    /// <summary>obf: <c>ik.i(key)</c> — get string, empty if missing.</summary>
    public string GetString(string key)
        => _map.TryGetValue(key, out var t) && t is NbtString s ? s.Value : "";

    /// <summary>obf: <c>ik.j(key)</c> — get byte array, empty if missing.</summary>
    public byte[] GetByteArray(string key)
        => _map.TryGetValue(key, out var t) && t is NbtByteArray ba ? ba.Value : [];

    /// <summary>obf: <c>ik.m(key)</c> — get boolean (TAG_Byte != 0).</summary>
    public bool GetBoolean(string key) => GetByte(key) != 0;

    /// <summary>obf: <c>ik.k(key)</c> — get TAG_Compound, null if missing or wrong type.</summary>
    public NbtCompound? GetCompound(string key)
        => _map.TryGetValue(key, out var t) && t is NbtCompound c ? c : null;

    public NbtList? GetList(string key)
        => _map.TryGetValue(key, out var t) && t is NbtList l ? l : null;

    // ── Wire encoding ─────────────────────────────────────────────────────────

    internal override void WritePayload(NbtBinaryWriter w)
    {
        foreach (var (name, tag) in _map)
        {
            w.WriteByte(tag.TypeId);
            w.WriteUtf(name);
            tag.WritePayload(w);
        }
        w.WriteByte(0); // TAG_End terminator
    }
}

// ── Low-level big-endian writer ───────────────────────────────────────────────

/// <summary>
/// Thin wrapper around a <see cref="Stream"/> that writes Java DataOutput big-endian values.
/// </summary>
internal sealed class NbtBinaryWriter(Stream stream)
{
    private readonly Stream _s = stream;

    public void WriteByte(byte v)  => _s.WriteByte(v);

    public void WriteShort(short v)
    {
        _s.WriteByte((byte)(v >> 8));
        _s.WriteByte((byte)v);
    }

    public void WriteInt(int v)
    {
        _s.WriteByte((byte)(v >> 24));
        _s.WriteByte((byte)(v >> 16));
        _s.WriteByte((byte)(v >> 8));
        _s.WriteByte((byte)v);
    }

    public void WriteLong(long v)
    {
        WriteInt((int)(v >> 32));
        WriteInt((int)v);
    }

    public void WriteBytes(byte[] data) => _s.Write(data, 0, data.Length);

    /// <summary>Java DataOutput.writeUTF: 2-byte big-endian length + UTF-8 bytes.</summary>
    public void WriteUtf(string value)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        WriteShort((short)bytes.Length);
        WriteBytes(bytes);
    }
}

// ── Low-level big-endian reader ───────────────────────────────────────────────

/// <summary>
/// Thin wrapper around a <see cref="Stream"/> that reads Java DataInput big-endian values.
/// </summary>
internal sealed class NbtBinaryReader(Stream stream)
{
    private readonly Stream _s = stream;

    public byte ReadByte()
    {
        int b = _s.ReadByte();
        if (b < 0) throw new EndOfStreamException("NBT stream ended unexpectedly");
        return (byte)b;
    }

    public short ReadShort() => (short)((ReadByte() << 8) | ReadByte());

    public int ReadInt()
        => (ReadByte() << 24) | (ReadByte() << 16) | (ReadByte() << 8) | ReadByte();

    public long ReadLong()
    {
        long hi = (uint)ReadInt();
        long lo = (uint)ReadInt();
        return (hi << 32) | lo;
    }

    public float  ReadFloat()  => BitConverter.Int32BitsToSingle(ReadInt());
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());

    public byte[] ReadBytes(int count)
    {
        byte[] buf = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = _s.Read(buf, offset, count - offset);
            if (read == 0) throw new EndOfStreamException("NBT stream ended inside byte array");
            offset += read;
        }
        return buf;
    }

    /// <summary>Java DataInput.readUTF: 2-byte unsigned length + UTF-8 bytes.</summary>
    public string ReadUtf()
    {
        int len = (ushort)ReadShort();
        return System.Text.Encoding.UTF8.GetString(ReadBytes(len));
    }
}
