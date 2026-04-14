using System.IO.Compression;

namespace SpectraSharp.Core.Nbt;

/// <summary>
/// Static NBT serializer. Replica of <c>vx</c> (NbtIo).
///
/// File-level framing: all .dat files (chunks, level.dat) are GZip-compressed.
/// Root element is always a named TAG_Compound (type byte 0x0A, then writeUTF name, then payload).
///
/// vx.a(DataInput)       → Read from GZipped stream
/// vx.a(ik, File)        → Write to GZipped file (atomic: tmp + rename)
/// vx.b(ik, File)        → Write plain (no GZip) — auxiliary files
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/WorldSave_Spec.md §5
/// </summary>
public static class NbtIo
{
    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a GZip-compressed NBT file. Returns the root TAG_Compound.
    /// Spec: <c>vx.a(FileInputStream)</c>.
    /// </summary>
    public static NbtCompound Read(string path)
    {
        using var fs = File.OpenRead(path);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);
        return ReadStream(gz);
    }

    /// <summary>
    /// Writes a GZip-compressed NBT file atomically (write to <c>path_tmp</c>, rename).
    /// Spec: <c>vx.a(ik, File)</c>.
    /// </summary>
    public static void Write(NbtCompound root, string path)
    {
        string tmp = path + "_tmp";
        using (var fs = File.Create(tmp))
        using (var gz = new GZipStream(fs, CompressionLevel.Optimal))
            WriteStream(root, gz);

        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
        if (File.Exists(tmp)) File.Delete(tmp); // clean up if rename failed silently
    }

    /// <summary>
    /// Writes a plain (non-GZipped) NBT file.
    /// Spec: <c>vx.b(ik, File)</c> — used for auxiliary .dat files.
    /// </summary>
    public static void WritePlain(NbtCompound root, string path)
    {
        using var fs = File.Create(path);
        WriteStream(root, fs);
    }

    // ── Stream-level I/O ──────────────────────────────────────────────────────

    /// <summary>
    /// Reads from an already-opened (and possibly decompressed) stream.
    /// Reads exactly one named tag and asserts it is a TAG_Compound.
    /// </summary>
    public static NbtCompound ReadStream(Stream stream)
    {
        var reader = new NbtBinaryReader(stream);
        byte typeId = reader.ReadByte();
        if (typeId != 10)
            throw new InvalidDataException($"NBT root must be TAG_Compound (10), got {typeId}");
        _ = reader.ReadUtf(); // root compound name (usually "" or ignored)
        return ReadCompoundPayload(reader);
    }

    /// <summary>
    /// Writes to an already-opened stream. Root tag framing: type byte + writeUTF name + payload.
    /// </summary>
    public static void WriteStream(NbtCompound root, Stream stream)
    {
        var writer = new NbtBinaryWriter(stream);
        writer.WriteByte(root.TypeId);  // 0x0A
        writer.WriteUtf("");            // root compound has empty name by convention
        root.WritePayload(writer);
    }

    // ── Internal read helpers ─────────────────────────────────────────────────

    private static NbtTag ReadPayload(byte typeId, NbtBinaryReader r) => typeId switch
    {
        1  => new NbtByte(r.ReadByte()),
        2  => new NbtShort(r.ReadShort()),
        3  => new NbtInt(r.ReadInt()),
        4  => new NbtLong(r.ReadLong()),
        5  => new NbtFloat(r.ReadFloat()),
        6  => new NbtDouble(r.ReadDouble()),
        7  => ReadByteArray(r),
        8  => new NbtString(r.ReadUtf()),
        9  => ReadListPayload(r),
        10 => ReadCompoundPayload(r),
        _  => throw new InvalidDataException($"Unknown NBT tag type: {typeId}")
    };

    private static NbtByteArray ReadByteArray(NbtBinaryReader r)
    {
        int len  = r.ReadInt();
        byte[] data = r.ReadBytes(len);
        return new NbtByteArray(data);
    }

    private static NbtList ReadListPayload(NbtBinaryReader r)
    {
        byte elementType = r.ReadByte();
        int  count       = r.ReadInt();
        var  list        = new NbtList();
        for (int i = 0; i < count; i++)
            list.Add(ReadPayload(elementType, r));
        return list;
    }

    private static NbtCompound ReadCompoundPayload(NbtBinaryReader r)
    {
        var compound = new NbtCompound();
        while (true)
        {
            byte typeId = r.ReadByte();
            if (typeId == 0) break; // TAG_End
            string name = r.ReadUtf();
            NbtTag tag  = ReadPayload(typeId, r);
            compound.Put(name, tag);
        }
        return compound;
    }
}
