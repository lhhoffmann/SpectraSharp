# SpectraSharp Parity Documentation

This folder is the communication hub between the two AIs in the clean-room workflow.

## Workflow

```
Analysis AI                        Coder AI
(reads decompiled code)            (reads specs only, never sees original)
        |                                  |
        v                                  v
   Specs/*.md  ──────────────────►  implements C# from spec
                                           |
                                           v
                                     REQUESTS.md
                                   (asks for missing specs)
```

## Index

### Specs

| File | Subject | Status |
|---|---|---|
| [MathHelper_Spec.md](Specs/MathHelper_Spec.md) | Sine/cosine lookup table + numeric helpers (floor, sqrt, clamp, abs, floor-division, RNG range) | Ready |

### Mappings

| File | Description |
|---|---|
| [Mappings/classes.md](Mappings/classes.md) | Obfuscated class name → human-readable name |

## How to add a spec

1. Analysis AI writes `Specs/<Topic>.md` following the spec template.
2. Analysis AI adds a row to the table above.
3. Coder AI picks it up and crosses off the corresponding REQUESTS.md entry.
