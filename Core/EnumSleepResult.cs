namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>qy</c> (EnumStatus / EnumSleepResult) — result of a sleep attempt.
///
/// Returned by <see cref="EntityPlayer.TrySleep"/> and consumed by
/// <see cref="BlockBed"/> to decide what message (if any) to send to the player.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/BlockBed_Spec.md §16
/// </summary>
public enum EnumSleepResult
{
    /// <summary>obf: qy.a — Sleep started successfully.</summary>
    Ok,

    /// <summary>obf: qy.b — Wrong dimension (Nether / End). WorldProvider.c==true.</summary>
    WrongDimension,

    /// <summary>obf: qy.c — Not night time. World.IsDaytime() is true. Message: tile.bed.noSleep.</summary>
    NotNight,

    /// <summary>obf: qy.d — Player is too far from the bed (>3 XZ or >2 Y).</summary>
    TooFar,

    /// <summary>obf: qy.e — Player is already sleeping or is dead.</summary>
    AlreadySleeping,

    /// <summary>obf: qy.f — Monsters nearby within ±8 XZ / ±5 Y of the bed. Message: tile.bed.notSafe.</summary>
    NotSafe,
}
