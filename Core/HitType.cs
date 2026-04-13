namespace SpectraSharp.Core;

/// <summary>
/// Replica of <c>bo</c> (EnumMovingObjectType) — discriminates block hits from entity hits
/// in <see cref="MovingObjectPosition"/>.
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/MovingObjectPosition_Spec.md §4
/// Note: Whether the original is a Java enum or static constants is an open question
/// (see REQUESTS.md). In C# an enum is the canonical representation.
/// </summary>
public enum HitType
{
    /// <summary>bo.a — ray hit a block/tile face.</summary>
    Tile   = 0,

    /// <summary>bo.b — ray hit an entity.</summary>
    Entity = 1,
}
