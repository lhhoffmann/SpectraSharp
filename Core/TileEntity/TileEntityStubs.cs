namespace SpectraSharp.Core.TileEntity;

/// <summary>
/// Stub tile entities that are registered in the factory but have no additional NBT
/// beyond the base id/x/y/z fields. See TileEntity_Spec §11.
/// </summary>

/// <summary>Brewing stand TE. obf: <c>tt</c>. Registry: "Cauldron".</summary>
public sealed class TileEntityBrewingStand : TileEntity { }

/// <summary>Enchanting table TE. obf: <c>rq</c>. Registry: "EnchantTable".</summary>
public sealed class TileEntityEnchantTable : TileEntity { }

/// <summary>End portal frame TE. obf: <c>yg</c>. Registry: "Airportal".</summary>
public sealed class TileEntityEndPortal : TileEntity { }
