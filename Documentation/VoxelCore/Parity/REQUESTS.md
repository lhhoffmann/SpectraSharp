# Coder AI — Open Requests

The coder AI adds entries here when it needs a spec that does not yet exist.
The analysis AI works through this list and creates the corresponding file in `Specs/`.

## Format

```
## <Topic>
**Needed for:** <which C# class/feature needs this>
**Questions:** <specific unknowns that block implementation>
```

---

## MathHelper
[STATUS:PROVIDED]
**Needed for:** `Core/MathHelper.cs` — deterministic trig used by physics, entity movement, world-gen
**Questions:**
- Sine table: how many entries? (suspected 65536, but must be confirmed)
- Is the table indexed as `sin[(int)(angle * TABLE_SIZE / TAU) & MASK]` or different?
- Does the class expose `cos` as a phase-shifted sine lookup or a separate table?
- Are there any other constants or helper methods (e.g. `floor`, `abs`, clamp) that must match Java's behaviour exactly?
- Which Java class name does this map to? (`net.minecraft.src.MathHelper`? or `Mth`?)

## JavaRandom
[STATUS:IMPLEMENTED]
**Needed for:** `Core/JavaRandom.cs`
**Notes:** Implemented from Java SE public specification (LCG algorithm is normative). No decompiled source consulted.
