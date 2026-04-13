namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>up</c> — 4-bits-per-block nibble array used by <see cref="Chunk"/>
/// for block metadata, sky-light, and block-light storage.
///
/// Backing store: <c>byte[] a</c> of length <c>size &gt;&gt; 1 = 16384</c>
/// (32768 indices × 4 bits = 16384 bytes).
///
/// Block-array index formula (same as Chunk block array):
///   <c>index = (localX &lt;&lt; 11) | (localZ &lt;&lt; 7) | localY</c>
///
/// Nibble packing — even index = low 4 bits, odd index = high 4 bits:
///   Read:  even → <c>a[i&gt;&gt;1] &amp; 0xF</c>;  odd → <c>(a[i&gt;&gt;1] &gt;&gt; 4) &amp; 0xF</c>
///   Write: even → <c>a[i&gt;&gt;1] = (a[i&gt;&gt;1] &amp; 0xF0) | (v &amp; 0xF)</c>;
///           odd → <c>a[i&gt;&gt;1] = (a[i&gt;&gt;1] &amp; 0x0F) | ((v &amp; 0xF) &lt;&lt; 4)</c>
///
/// [UNCERTAIN] nibble packing convention — confirmed as "expected" from Chunk_Spec.md §16
/// open question 1 but not verified from source. Standard Minecraft convention used.
///
/// Source spec: Chunk_Spec.md §5, §16
/// </summary>
public sealed class NibbleArray
{
    private readonly byte[] _data; // obf: a  — backing nibble byte array

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <summary>
    /// Spec: <c>up(int size, int heightBits)</c>.
    /// size = 32768, heightBits = 7 (world.a). Backing array = size &gt;&gt; 1 = 16384 bytes.
    /// </summary>
    public NibbleArray(int size, int heightBits)
    {
        _data = new byte[size >> 1];
    }

    // ── Read / write ──────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the 4-bit value at chunk-local (x, y, z). Spec: <c>a(x,y,z)</c> → int.
    /// </summary>
    public int Get(int x, int y, int z)
    {
        int index = (x << 11) | (z << 7) | y;
        return (index & 1) == 0
            ? _data[index >> 1] & 0xF
            : (_data[index >> 1] >> 4) & 0xF;
    }

    /// <summary>
    /// Writes the 4-bit value at chunk-local (x, y, z). Spec: <c>a(x,y,z,value)</c>.
    /// </summary>
    public void Set(int x, int y, int z, int value)
    {
        int index = (x << 11) | (z << 7) | y;
        int half  = index >> 1;
        if ((index & 1) == 0)
            _data[half] = (byte)((_data[half] & 0xF0) | (value & 0xF));
        else
            _data[half] = (byte)((_data[half] & 0x0F) | ((value & 0xF) << 4));
    }

    /// <summary>Returns the raw backing byte array for serialisation / network packets.</summary>
    public byte[] GetData() => _data;
}
