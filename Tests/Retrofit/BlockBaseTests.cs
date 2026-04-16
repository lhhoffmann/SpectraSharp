using System;
using System.Collections.Generic;
using System.Numerics;
using Xunit;
using SpectraEngine.Core;
using SpectraEngine.Core.Mods;
using SpectraEngine.Bridge.Overrides;

namespace SpectraEngine.Tests.Bridge.Overrides
{
    // ── Hand-written fakes ────────────────────────────────────────────────────

    public interface IWorld { }

    // Minimal concrete implementation for testing BlockBase
    internal class ConcreteBlock : BlockBase
    {
        public override int BlockId => 42;
        public override int TextureIndex => 7;

        // Expose protected members for testing
        public float ExposedHardness => Hardness;
        public float ExposedBlastResistance => BlastResistance;
        public new void CallBlockTick(double delta) => BlockTick(delta);
        public bool BlockTickCalled { get; private set; }
        protected override void BlockTick(double deltaSeconds) { BlockTickCalled = true; }
    }

    internal class ConcreteBlockWithOverrides : BlockBase
    {
        public override int BlockId => 1;
        public override int TextureIndex => 10;
        public override int TextureIndexTop => 20;
        public override int TextureIndexSide => 30;
        public override int TextureIndexBottom => 40;
    }

    internal class ConcreteBlockCustomDrops : BlockBase
    {
        public override int BlockId => 5;
        public override int TextureIndex => 3;

        public override IEnumerable<SpectraEngine.Core.Mods.ItemStack> GetDrops(int meta, Random rng)
        {
            yield return new SpectraEngine.Core.Mods.ItemStack(BlockId, 2);
            yield return new SpectraEngine.Core.Mods.ItemStack(BlockId + 1, 1);
        }
    }

    internal class ConcreteBlockNoTickOverride : BlockBase
    {
        public override int BlockId => 9;
        public override int TextureIndex => 2;
    }

    // ── Test class ────────────────────────────────────────────────────────────

    public class BlockBaseTests
    {
        // ── Priority ─────────────────────────────────────────────────────────

        [Fact]
        public void Priority_IsAlways10()
        {
            var block = new ConcreteBlock();
            Assert.Equal(10, block.Priority);
        }

        // ── Defaults — Hardness and BlastResistance ───────────────────────────

        [Fact]
        public void Hardness_DefaultIs1()
        {
            var block = new ConcreteBlock();
            Assert.Equal(1.0f, block.ExposedHardness);
        }

        [Fact]
        public void BlastResistance_DefaultIs1()
        {
            var block = new ConcreteBlock();
            Assert.Equal(1.0f, block.ExposedBlastResistance);
        }

        // ── BlockId ───────────────────────────────────────────────────────────

        [Fact]
        public void BlockId_ReturnsConcreteValue()
        {
            var block = new ConcreteBlock();
            Assert.Equal(42, block.BlockId);
        }

        // ── TextureIndex defaults ─────────────────────────────────────────────

        [Fact]
        public void TextureIndexTop_DefaultsToTextureIndex()
        {
            var block = new ConcreteBlock();
            Assert.Equal(block.TextureIndex, block.TextureIndexTop);
        }

        [Fact]
        public void TextureIndexSide_DefaultsToTextureIndex()
        {
            var block = new ConcreteBlock();
            Assert.Equal(block.TextureIndex, block.TextureIndexSide);
        }

        [Fact]
        public void TextureIndexBottom_DefaultsToTextureIndex()
        {
            var block = new ConcreteBlock();
            Assert.Equal(block.TextureIndex, block.TextureIndexBottom);
        }

        // ── TextureIndex overrides ────────────────────────────────────────────

        [Fact]
        public void TextureIndexTop_CanBeOverridden()
        {
            var block = new ConcreteBlockWithOverrides();
            Assert.Equal(20, block.TextureIndexTop);
        }

        [Fact]
        public void TextureIndexSide_CanBeOverridden()
        {
            var block = new ConcreteBlockWithOverrides();
            Assert.Equal(30, block.TextureIndexSide);
        }

        [Fact]
        public void TextureIndexBottom_CanBeOverridden()
        {
            var block = new ConcreteBlockWithOverrides();
            Assert.Equal(40, block.TextureIndexBottom);
        }

        // ── TextureKey formatting ─────────────────────────────────────────────

        [Fact]
        public void TextureKey_FormatsAsBlockUnderscore_Index()
        {
            var block = new ConcreteBlock();
            Assert.Equal($"block_{block.TextureIndex}", block.TextureKey);
        }

        [Fact]
        public void TextureKeyTop_FormatsCorrectly()
        {
            var block = new ConcreteBlock();
            Assert.Equal($"block_{block.TextureIndexTop}", block.TextureKeyTop);
        }

        [Fact]
        public void TextureKeySide_FormatsCorrectly()
        {
            var block = new ConcreteBlock();
            Assert.Equal($"block_{block.TextureIndexSide}", block.TextureKeySide);
        }

        [Fact]
        public void TextureKeyBottom_FormatsCorrectly()
        {
            var block = new ConcreteBlock();
            Assert.Equal($"block_{block.TextureIndexBottom}", block.TextureKeyBottom);
        }

        [Fact]
        public void TextureKeyTop_UsesOverriddenIndex_WhenOverridden()
        {
            var block = new ConcreteBlockWithOverrides();
            Assert.Equal("block_20", block.TextureKeyTop);
        }

        [Fact]
        public void TextureKeySide_UsesOverriddenIndex_WhenOverridden()
        {
            var block = new ConcreteBlockWithOverrides();
            Assert.Equal("block_30", block.TextureKeySide);
        }

        [Fact]
        public void TextureKeyBottom_UsesOverriddenIndex_WhenOverridden()
        {
            var block = new ConcreteBlockWithOverrides();
            Assert.Equal("block_40", block.TextureKeyBottom);
        }

        // ── RenderColor default ───────────────────────────────────────────────

        [Fact]
        public void RenderColor_DefaultIs_200_200_200_255()
        {
            var block = new ConcreteBlock();
            var color = block.RenderColor;
            Assert.Equal(200, color.R);
            Assert.Equal(200, color.G);
            Assert.Equal(200, color.B);
            Assert.Equal(255, color.A);
        }

        // ── BiomeTintColor default ────────────────────────────────────────────

        [Fact]
        public void BiomeTintColor_DefaultIsWhite_255_255_255_255()
        {
            var block = new ConcreteBlock();
            var tint = block.BiomeTintColor;
            Assert.Equal(255, tint.R);
            Assert.Equal(255, tint.G);
            Assert.Equal(255, tint.B);
            Assert.Equal(255, tint.A);
        }

        // ── Position ──────────────────────────────────────────────────────────

        [Fact]
        public void Position_DefaultIsZero()
        {
            var block = new ConcreteBlock();
            Assert.Equal(Vector3.Zero, block.Position);
        }

        [Fact]
        public void Position_CanBeSet()
        {
            var block = new ConcreteBlock();
            var pos = new Vector3(3f, 64f, -7f);
            block.Position = pos;
            Assert.Equal(pos, block.Position);
        }

        // ── TickCount ─────────────────────────────────────────────────────────

        [Fact]
        public void TickCount_StartsAtZero()
        {
            var block = new ConcreteBlock();
            Assert.Equal(0L, block.TickCount);
        }

        [Fact]
        public void TickCount_IncrementsOnEachOnTick()
        {
            var block = new ConcreteNoTickOverrideBlock();
            block.OnTick(0.05);
            Assert.Equal(1L, block.TickCount);
            block.OnTick(0.05);
            Assert.Equal(2L, block.TickCount);
            block.OnTick(0.05);
            Assert.Equal(3L, block.TickCount);
        }

        [Fact]
        public void TickCount_IncrementsBy1PerCall_NotByDelta()
        {
            var block = new ConcreteNoTickOverrideBlock();
            block.OnTick(999.0); // large delta should not matter
            Assert.Equal(1L, block.TickCount);
        }

        // ── OnTick delegates to BlockTick ─────────────────────────────────────

        [Fact]
        public void OnTick_InvokesBlockTick()
        {
            var block = new ConcreteBlock();
            Assert.False(block.BlockTickCalled);
            block.OnTick(0.05);
            Assert.True(block.BlockTickCalled);
        }

        [Fact]
        public void OnTick_PassesDeltaSecondsToBlockTick()
        {
            var block = new DeltaCapturingBlock();
            block.OnTick(0.05);
            Assert.Equal(0.05, block.LastDelta, precision: 10);
        }

        [Fact]
        public void OnTick_IncrementsTickCountBeforeBlockTick()
        {
            var block = new TickCountOrderBlock();
            block.OnTick(0.05);
            // TickCount should be 1 at the time BlockTick is called
            Assert.Equal(1L, block.TickCountDuringBlockTick);
        }

        // ── OnUse default ─────────────────────────────────────────────────────

        [Fact]
        public void OnUse_ReturnsFalseByDefault()
        {
            var block = new ConcreteBlock();
            var result = block.OnUse(null, null, 0, 0, 0, Face.Top);
            Assert.False(result);
        }

        [Fact]
        public void OnUse_ReturnsFalseForAllFaces_ByDefault()
        {
            var block = new ConcreteBlock();
            foreach (Face face in Enum.GetValues(typeof(Face)))
            {
                Assert.False(block.OnUse(null, null, 10, 64, -5, face));
            }
        }

        // ── GetDrops default ──────────────────────────────────────────────────

        [Fact]
        public void GetDrops_DefaultReturnsSingleStack_WithBlockId_AndCount1()
        {
            var block = new ConcreteBlock();
            var rng = new Random(12345);
            var drops = new List<SpectraEngine.Core.Mods.ItemStack>(block.GetDrops(0, rng));
            Assert.Single(drops);
            Assert.Equal(block.BlockId, drops[0].Id);
            Assert.Equal(1, drops[0].Count);
        }

        [Fact]
        public void GetDrops_DefaultUsesBlockId_NotHardcodedValue()
        {
            var block = new ConcreteBlockWithOverrides();
            var rng = new Random(99);
            var drops = new List<SpectraEngine.Core.Mods.ItemStack>(block.GetDrops(0, rng));
            Assert.Single(drops);
            Assert.Equal(1, drops[0].Id); // BlockId == 1
        }

        [Fact]
        public void GetDrops_MetaDoesNotAffectDefaultDrops()
        {
            var block = new ConcreteBlock();
            var rng = new Random(0);
            var drops0 = new List<SpectraEngine.Core.Mods.ItemStack>(block.GetDrops(0, rng));
            rng = new Random(0);
            var drops15 = new List<SpectraEngine.Core.Mods.ItemStack>(block.GetDrops(15, rng));
            Assert.Equal(drops0.Count, drops15.Count);
            Assert.Equal(drops0[0].Id, drops15[0].Id);
            Assert.Equal(drops0[0].Count, drops15[0].Count);
        }

        [Fact]
        public void GetDrops_CanBeOverriddenToReturnMultipleStacks()
        {
            var block = new ConcreteBlockCustomDrops();
            var rng = new Random(42);
            var drops = new List<SpectraEngine.Core.Mods.ItemStack>(block.GetDrops(0, rng));
            Assert.Equal(2, drops.Count);
        }

        // ── Multiple ticks accumulate correctly ───────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(20)]
        [InlineData(100)]
        [InlineData(1200)]
        public void TickCount_AccumulatesCorrectlyOverMultipleTicks(int tickCount)
        {
            var block = new ConcreteNoTickOverrideBlock();
            for (int i = 0; i < tickCount; i++)
                block.OnTick(0.05);
            Assert.Equal((long)tickCount, block.TickCount);
        }

        // ── TextureKey consistency with index ─────────────────────────────────

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(255)]
        public void TextureKey_AlwaysMatchesPattern_block_N(int index)
        {
            var block = new VariableIndexBlock(index);
            Assert.Equal($"block_{index}", block.TextureKey);
            Assert.Equal($"block_{index}", block.TextureKeyTop);
            Assert.Equal($"block_{index}", block.TextureKeySide);
            Assert.Equal($"block_{index}", block.TextureKeyBottom);
        }

        // ── Helper inner classes ───────────────────────────────────────────────

        private class ConcreteNoTickOverrideBlock : BlockBase
        {
            public override int BlockId => 3;
            public override int TextureIndex => 1;
        }

        private class DeltaCapturingBlock : BlockBase
        {
            public override int BlockId => 7;
            public override int TextureIndex => 4;
            public double LastDelta { get; private set; }
            protected override void BlockTick(double deltaSeconds) { LastDelta = deltaSeconds; }
        }

        private class TickCountOrderBlock : BlockBase
        {
            public override int BlockId => 8;
            public override int TextureIndex => 5;
            public long TickCountDuringBlockTick { get; private set; }
            protected override void BlockTick(double deltaSeconds) { TickCountDuringBlockTick = TickCount; }
        }

        private class VariableIndexBlock : BlockBase
        {
            private readonly int _index;
            public VariableIndexBlock(int index) { _index = index; }
            public override int BlockId => 0;
            public override int TextureIndex => _index;
        }
    }
}