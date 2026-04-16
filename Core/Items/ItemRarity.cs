namespace SpectraEngine.Core.Items;

/// <summary>
/// Replica of <c>ja</c> — item rarity enum controlling tooltip name colour.
/// Used by <see cref="ItemRecord.GetRarity"/> and potentially future enchanted/special items.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/ItemRecord_Jukebox_Spec.md §4.3
/// </summary>
public enum ItemRarity
{
    /// <summary>obf: ja.a — common (white tooltip).</summary>
    Common,

    /// <summary>obf: ja.b — uncommon (yellow tooltip).</summary>
    Uncommon,

    /// <summary>obf: ja.c — rare (aqua/light-blue tooltip).</summary>
    Rare,

    /// <summary>obf: ja.d — epic (light-purple tooltip).</summary>
    Epic,
}
