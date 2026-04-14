namespace SpectraSharp.Bridge.Overrides;

// Blocks with no custom behaviour — only a Java class name and a terrain.png tile index.
// BridgeRegistry discovers these automatically via reflection at boot; no registration needed.
//
// Tile index layout (terrain.png, 16x16 grid of 16x16 tiles, left-to-right top-to-bottom):
//   col = index % 16,  row = index / 16
//
// To add a block: one line here, done. For blocks with tick logic or drops, use a separate file.

sealed class GrassBlock    : BlockBase {
    public override int    BlockId            => 2;
    public override string JavaClassName      => "net.minecraft.src.BlockGrass";
    public override int    TextureIndex       => 0;  // top (grass_top, gray → biome tinted)
    public override int    TextureIndexSide   => 3;  // sides (grass_side gradient → biome tinted)
    public override int    TextureIndexBottom => 2;  // bottom = dirt (no tint)
    public override Raylib_cs.Color BiomeTintColor {
        get {
            int rgb = SpectraSharp.Core.GrassColorizer.GetGrassColor(0.8, 0.4);
            return new Raylib_cs.Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)255);
        }
    }
}
sealed class DirtBlock     : BlockBase { public override int BlockId =>  3; public override string JavaClassName => "net.minecraft.src.BlockDirt";        public override int TextureIndex =>  2; }
sealed class WoodBlock     : BlockBase {
    public override int    BlockId            => 17;
    public override string JavaClassName      => "net.minecraft.src.BlockLog";
    public override int    TextureIndex       => 20; // sides = oak bark
    public override int    TextureIndexTop    => 21; // top  = log end
    public override int    TextureIndexBottom => 21; // bottom = log end
}
sealed class LeavesBlock   : BlockBase {
    public override int    BlockId       => 18;
    public override string JavaClassName => "net.minecraft.src.BlockLeaves";
    public override int    TextureIndex  => 52;
    public override Raylib_cs.Color BiomeTintColor {
        get {
            int rgb = SpectraSharp.Core.FoliageColorizer.GetFoliageColor(0.7, 0.8);
            return new Raylib_cs.Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)255);
        }
    }
}
sealed class SandBlock     : BlockBase { public override int BlockId => 12; public override string JavaClassName => "net.minecraft.src.BlockSand";        public override int TextureIndex => 18; }
sealed class GravelBlock   : BlockBase { public override int BlockId => 13; public override string JavaClassName => "net.minecraft.src.BlockGravel";      public override int TextureIndex => 19; }
sealed class GoldOreBlock  : BlockBase { public override int BlockId => 14; public override string JavaClassName => "net.minecraft.src.BlockOreGold";     public override int TextureIndex => 32; }
sealed class IronOreBlock  : BlockBase { public override int BlockId => 15; public override string JavaClassName => "net.minecraft.src.BlockOreIron";     public override int TextureIndex => 33; }
sealed class CoalOreBlock  : BlockBase { public override int BlockId => 16; public override string JavaClassName => "net.minecraft.src.BlockOreCoal";     public override int TextureIndex => 34; }
sealed class PlankBlock    : BlockBase { public override int BlockId =>  5; public override string JavaClassName => "net.minecraft.src.BlockWood";        public override int TextureIndex =>  4; }
sealed class CobbleBlock   : BlockBase { public override int BlockId =>  4; public override string JavaClassName => "net.minecraft.src.BlockCobblestone"; public override int TextureIndex => 16; }
sealed class BedrockBlock  : BlockBase { public override int BlockId =>  7; public override string JavaClassName => "net.minecraft.src.BlockBedrock";     public override int TextureIndex => 17; }
sealed class GlassBlock    : BlockBase { public override int BlockId => 20; public override string JavaClassName => "net.minecraft.src.BlockGlass";       public override int TextureIndex => 49; }
