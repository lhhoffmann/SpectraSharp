# Documentation License

## Code vs. Documentation

This repository uses a dual-license model:

| Content | License |
|---|---|
| Source code (`*.cs`, `*.csproj`, build scripts) | GNU AGPL v3 (see `/LICENSE`) |
| Documentation (`Documentation/**/*.md`) | Creative Commons Attribution 4.0 International (CC BY 4.0) |

---

## Creative Commons Attribution 4.0 International (CC BY 4.0)

Copyright © 2026 lhhoffmann / SpectraSharp Contributors

You are free to:
- **Share** — copy and redistribute this material in any medium or format
- **Adapt** — remix, transform, and build upon this material for any purpose

Under the following terms:
- **Attribution** — You must give appropriate credit to the **SpectraSharp project**
  (`github.com/lhhoffmann/SpectraSharp`), provide a link to the license, and indicate
  if changes were made.

No additional restrictions — you may not apply legal terms or technological measures
that legally restrict others from doing anything the license permits.

Full license text: https://creativecommons.org/licenses/by/4.0/legalcode

---

## What is and is not covered

### Protected by this license (original work)
- The **structure and organisation** of these specification documents
- The **prose descriptions**, logic-flow narratives, and step-by-step explanations
- The **mapping tables** between obfuscated names and human-readable names
- The **analysis methodology** and documentation templates

### Not protected (functional facts, not copyrightable)
- Raw numeric constants (e.g. block IDs, hardness values, texture indices)
- Mathematical formulas derived from observable game behaviour
- Java class names and method signatures (technical identifiers, not creative expression)

### Explicitly excluded from this repository
- No decompiled Java source code is included in the `Documentation/` folder.
- All specifications are written descriptions of observed behaviour, not copies of source code.

---

## Attribution requirement for derived engines

If you use these specifications to build a compatible engine or tool, include the following
notice in your project's documentation or about screen:

> Parity specifications based on the SpectraSharp project
> (github.com/lhhoffmann/SpectraSharp), licensed under CC BY 4.0.
