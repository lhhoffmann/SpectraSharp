namespace SpectraSharp.Bridge.Overrides;

// Blocks with no custom behaviour — only a Java class name and a terrain.png tile index.
// BridgeRegistry discovers these automatically via reflection at boot; no registration needed.
//
// Tile index layout (terrain.png, 16x16 grid of 16x16 tiles, left-to-right top-to-bottom):
//   col = index % 16,  row = index / 16
//
// To add a block: one line here, done. For blocks with tick logic or drops, use a separate file.

sealed class GrassBlock    : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockGrass";       public override int TextureIndex =>  3; }
sealed class DirtBlock     : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockDirt";        public override int TextureIndex =>  2; }
sealed class WoodBlock     : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockLog";         public override int TextureIndex => 20; }
sealed class LeavesBlock   : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockLeaves";      public override int TextureIndex => 52; }
sealed class SandBlock     : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockSand";        public override int TextureIndex => 18; }
sealed class GravelBlock   : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockGravel";      public override int TextureIndex => 19; }
sealed class GoldOreBlock  : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockOreGold";     public override int TextureIndex => 32; }
sealed class IronOreBlock  : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockOreIron";     public override int TextureIndex => 33; }
sealed class CoalOreBlock  : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockOreCoal";     public override int TextureIndex => 34; }
sealed class PlankBlock    : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockWood";        public override int TextureIndex =>  4; }
sealed class CobbleBlock   : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockCobblestone"; public override int TextureIndex => 16; }
sealed class BedrockBlock  : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockBedrock";     public override int TextureIndex => 17; }
sealed class GlassBlock    : BlockBase { public override string JavaClassName => "net.minecraft.src.BlockGlass";       public override int TextureIndex => 49; }
