# SpectraEngine — Engine Architecture & Extension Guide

## Table of Contents

1. [How the Engine is Layered](#1-how-the-engine-is-layered)
2. [Adding a New Block (Step by Step)](#2-adding-a-new-block-step-by-step)
3. [Adding a New Item](#3-adding-a-new-item)
4. [Extending to a New Version](#4-extending-to-a-new-version--how-it-works)
5. [SpectraEngine vs. Vanilla Java 1.0 — Benchmark Table](#5-SpectraEngine-vs-vanilla-java-10--benchmark-table)
6. [Module Checklist](#6-module-checklist)

---

## 1. How the Engine is Layered

```
┌──────────────────────────────────────────────────────────┐
│  Program.cs  — boot order only, no logic                 │
└────────────────────────┬─────────────────────────────────┘
                         │ constructs
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
  ┌────────────┐  ┌────────────┐  ┌────────────────────┐
  │  IO layer  │  │  Graphics  │  │  Bridge layer      │
  │            │  │  layer     │  │                    │
  │ AssetMgr   │  │ TextureReg │  │ BridgeRegistry     │
  │ VanillaEx  │  │ TerrainAtl │  │  ├─ Generated/     │
  └─────┬──────┘  └─────┬──────┘  │  └─ Overrides/     │
        │               │         └────────┬───────────┘
        └───────────────┴──────────────────┘
                         │ injected into
                         ▼
                  ┌─────────────┐
                  │ Core/Engine │  ← 20 Hz fixed tick + uncapped render
                  └─────────────┘
```

### Layer rules

| Layer | May import | Must NOT import |
|---|---|---|
| `IO` | nothing | Core, Graphics, Bridge |
| `Graphics` | IO (AssetData) | Core, Bridge |
| `Bridge` | IO, Graphics | Core directly |
| `Core` | IO, Graphics, Bridge | nothing above Core |

Cross-layer communication flows **downward only** through constructor injection.
There are no singletons, no static state, no service locators.

---

### The Bridge layer in detail

```
Bridge/
├── IBridgeStub.cs        ← interface + BridgeStubBase + BridgeRegistry
├── Generated/            ← transpiler output, never hand-edited
│   └── Compat_*.cs
└── Overrides/            ← hand-written, always Priority = 10
    ├── BlockBase.cs      ← abstract base for all blocks
    ├── StoneBlock.cs     ← concrete block example
    └── ItemBase.cs       ← (add this when items are needed)
```

`BridgeRegistry` discovers every class implementing `IBridgeStub` at boot via
reflection. When two stubs share the same `JavaClassName`, the one with higher
`Priority` wins. Generated stubs are `Priority = 0`; hand-written overrides are
`Priority = 10`.

---

## 2. Adding a New Block (Step by Step)

The entire change is **one new file**. Nothing else needs to be touched.

### Example: GrassBlock

Create `Bridge/Overrides/GrassBlock.cs`:

```csharp
namespace SpectraEngine.Bridge.Overrides;

/// <summary>
/// Parity override for net.minecraft.src.BlockGrass.
/// Terrain atlas tile index 0 = grass top face.
/// </summary>
public sealed class GrassBlock : BlockBase
{
    public override string JavaClassName => "net.minecraft.src.BlockGrass";

    // Row 0, column 0 of terrain.png
    public override int TextureIndex => 0;

    protected override void BlockTick(double deltaSeconds)
    {
        // Grass spreads to adjacent dirt — implement when world grid exists
    }
}
```

**That's it.** `BridgeRegistry` picks it up automatically at next boot. The
`TerrainAtlas` extracts tile 0 and registers it as `"block_0"` in `TextureRegistry`.

### Atlas index reference (terrain.png, 16×16 grid)

| Index | Block |
|---|---|
| 0 | Grass top |
| 1 | Stone |
| 2 | Dirt |
| 3 | Grass side |
| 4 | Wooden planks |
| 15 | Sand |
| 16 | Gravel |
| 17 | Gold ore |
| 18 | Iron ore |
| 19 | Coal ore |
| 20 | Log side |

Index formula: `col = index % 16`, `row = index / 16`.

---

## 3. Adding a New Item

Items follow the same pattern but derive from `ItemBase` instead of `BlockBase`.
`ItemBase` does not yet exist — here is how to add it:

**Step 1** — Create `Bridge/Overrides/ItemBase.cs`:

```csharp
namespace SpectraEngine.Bridge.Overrides;

public abstract class ItemBase : BridgeStubBase
{
    public override int Priority => 10;

    // items.png atlas index (separate texture from terrain.png)
    public abstract int ItemTextureIndex { get; }
    public string ItemTextureKey => $"item_{ItemTextureIndex}";

    public virtual int MaxStackSize => 64;
}
```

**Step 2** — Load `items.png` from the JAR in `AssetManager`:

```csharp
public AssetData ExtractItemsPng()
    => ExtractAsset("item/items.png");
```

**Step 3** — Create the concrete item (e.g. `Bridge/Overrides/WoodPickaxe.cs`):

```csharp
public sealed class WoodPickaxe : ItemBase
{
    public override string JavaClassName => "net.minecraft.src.ItemPickaxe";
    public override int ItemTextureIndex => 0;
    public override int MaxStackSize => 1;
}
```

---

## 4. Extending to a New Version — How it Works

The Bridge layer is designed so that adding a new game version never breaks the existing one.

### Pattern: versioned override folder

1. Create a subfolder — e.g. `Bridge/Overrides/v1_x/` — for the new version's stubs.
2. Concrete stubs in that folder use the **same `JavaClassName`** as the base version
   but declare `Priority = 20`, so they automatically win over base stubs (`Priority = 10`)
   without deleting them.
3. Flip a version flag in `Engine` (e.g. `GameVersion`) to activate that set.

The base stubs remain intact and can be used for regression comparison at any time.

### What typically changes between versions

| Area | Where to act |
|---|---|
| New block behaviour | New `BlockBase` subclass in the versioned subfolder |
| New item | New `ItemBase` subclass |
| Changed tick logic | Override stub at higher priority, leave original in place |
| New asset paths | Extend `AssetManager` with a new `Extract…` method |
| Changed argument format | Update `minecraftArguments` in the launcher JSON template |

### Nothing in `Core` or `Graphics` needs to change

Block and item logic lives entirely in the Bridge layer. `Engine.FixedUpdate` calls
`OnTick` on whatever stubs the `BridgeRegistry` holds — it is unaware of versions.

---

## 5. SpectraEngine vs. Vanilla Java 1.0 — Benchmark Table

> **Status: to be filled once the full 1.0 world render is running.**
> Methodology: same machine, same scene (flat world, render distance 8), averaged over 60 s.

| Metric | Vanilla Java 1.0 | SpectraEngine (Debug) | SpectraEngine (AOT) |
|---|---|---|---|
| Startup time | — ms | — ms | — ms |
| Idle frame time (20 chunks loaded) | — ms | — ms | — ms |
| Peak RAM (RSS) | — MB | — MB | — MB |
| Tick jitter (σ, 20 Hz) | — ms | — ms | — ms |
| GC pause max | — ms | 0 ms (AOT stack alloc) | 0 ms |
| CPU @ idle (single core) | —% | —% | —% |

Numbers will be measured with:
- Vanilla: `-Xmx512m -Xms512m`, Java 8, no mods
- SpectraEngine Debug: `dotnet run`, no AOT
- SpectraEngine AOT: `dotnet publish -r win-x64 -c Release`

---

## 6. Module Checklist

Use this when adding any new game object:

- [ ] Create one file in `Bridge/Overrides/` deriving from `BlockBase` or `ItemBase`
- [ ] Set `JavaClassName` to the correct fully-qualified Java identifier
- [ ] Set `TextureIndex` (blocks) or `ItemTextureIndex` (items)
- [ ] Implement `BlockTick` / item logic if the vanilla object has behaviour
- [ ] No changes needed to `Engine`, `BridgeRegistry`, `Program`, or any other file
- [ ] Build: `dotnet build` — zero warnings expected
- [ ] Verify the block/item appears in the engine log at boot (`[Bridge] registered: ...`)
