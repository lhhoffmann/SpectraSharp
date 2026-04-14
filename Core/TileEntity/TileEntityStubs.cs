namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Stub tile entities that are registered in the factory but have no additional NBT
/// beyond the base id/x/y/z fields. See TileEntity_Spec §11.
/// </summary>

/// <summary>Piston extension TE. obf: <c>agb</c>. Registry: "Piston".</summary>
public sealed class TileEntityPiston : TileEntity { }

/// <summary>Brewing stand TE. obf: <c>tt</c>. Registry: "Cauldron".</summary>
public sealed class TileEntityBrewingStand : TileEntity { }

/// <summary>Enchanting table TE. obf: <c>rq</c>. Registry: "EnchantTable".</summary>
public sealed class TileEntityEnchantTable : TileEntity { }

/// <summary>Jukebox / record player TE. obf: <c>agc</c>. Registry: "RecordPlayer".</summary>
public sealed class TileEntityRecordPlayer : TileEntity { }

/// <summary>End portal frame TE. obf: <c>yg</c>. Registry: "Airportal".</summary>
public sealed class TileEntityEndPortal : TileEntity { }
