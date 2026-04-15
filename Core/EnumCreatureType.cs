namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>jf</c> (EnumCreatureType) — classifies living entities for population-cap tracking.
///
/// Used by <see cref="SpawnerAnimals"/> to separate hostile, passive, and water mob populations.
///
/// Caps (spec §3):
///   Hostile  → 70 per 256 chunks
///   Passive  → 15 per 256 chunks
///   Water    →  5 per 256 chunks
/// </summary>
public enum EnumCreatureType
{
    /// <summary>obf: <c>jf.a</c> — hostile mobs (monsters). Base class: EntityMonster. Cap: 70.</summary>
    Hostile,

    /// <summary>obf: <c>jf.b</c> — passive land animals. Base class: EntityAnimal. Cap: 15.</summary>
    Passive,

    /// <summary>obf: <c>jf.c</c> — water creatures. Base class: EntityWaterMob. Cap: 5.</summary>
    Water,
}
