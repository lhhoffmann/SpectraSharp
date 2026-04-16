# Spec Header Template

Every file written to `Documentation/VoxelCore/Parity/Specs/` and
`Documentation/Mods/Specs/` MUST begin with this exact header block.
Copy it verbatim — only fill in the bracketed fields.

---

```markdown
<!--
  SpectraEngine Parity Documentation
  Copyright © 2026 lhhoffmann / SpectraEngine Contributors
  Licensed under CC BY 4.0 — https://creativecommons.org/licenses/by/4.0/
  See Documentation/LICENSE.md for full terms.

  CLEAN-ROOM NOTICE: This document contains no decompiled source code.
  All descriptions are original work derived from behavioural observation
  and structural analysis. Mathematical constants are functional facts
  and are not subject to copyright.
-->

# [Human Class Name] Spec
**Source class:** `[obfuscated].java`
**Superclass:** `[obfuscated]` (= `[HumanName]`) — or `none`
**Analyst:** lhhoffmann
**Date:** [YYYY-MM-DD]
**Status:** `DRAFT` | `REVIEWED` | `IMPLEMENTED`
**License:** [CC BY 4.0](../../../LICENSE.md)
```

---

## Why this header matters

1. **Copyright notice** — establishes authorship of the documentation at the file level,
   not just at the repo level. Harder to strip than a top-level LICENSE file.

2. **Clean-room notice** — on-record statement that this spec contains no copied code.
   If the document is ever used as evidence, it self-declares its methodology.

3. **CC BY 4.0 pointer** — anyone who extracts a single spec file still sees the license.

4. **Status field** — `DRAFT` → `REVIEWED` → `IMPLEMENTED` gives the Coder AI and
   contributors a clear signal about how trustworthy the spec is.
