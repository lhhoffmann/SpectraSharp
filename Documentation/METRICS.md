# SpectraSharp — Development Metrics

Log of all development sessions. One entry per session, appended at the bottom.
Written by the active role (Analyst / Coder / Mod Coder) at session end.

---

## Cumulative Costs

Update this table manually when a billing period ends or a charge appears.

| Date       | Item                  | Amount |
|------------|-----------------------|--------|
| 2026-04-13 | Claude Pro (monthly)  | 20 EUR |
| 2026-04-13 | Extra usage           | 13 EUR |
| 2026-04-13 | Extra usage           |  9 EUR |

**Running total: 42 EUR**

---

## Entry Format

```
## YYYY-MM-DD — [ROLE] — Topic

**Worked on:**
- Item 1
- Item 2

**Estimated effort:** ~N hours equivalent
**Notes:** (optional — decisions, blockers, open questions)
```

---

<!-- Entries below — newest at bottom -->

## 2026-04-13 — [ANALYST] — Item, LivingEntity, EntityItem, ItemStack, DataWatcher specs

**Worked on:**
- `cr` (DataWatcher) — type registry (7 types), wire format (typeId<<5|entryId header, 0x7F terminator), register/update/applyChanges methods, null-return quirk on read
- `ih` (EntityItem) — 0.25×0.25 size, gravity/bounce/friction tick, pickup delay quirk (set externally to 10), despawn at age ≥ 6000
- `dk` (ItemStack) — non-obvious field naming (c=itemId, a=stackSize, e=damage), two identical damage getters h()/i(), splitStack mutation, Unbreaking enchantment check
- `acy` (Item) — d[32000] registry with 256-offset, dual maxDamage paths (c() vs g()/a field), builder pattern, icon atlas packing, ray-cast helper, 12 virtual methods
- `nq` (LivingEntity) — health/invulnerability/armor-absorption system, friction movement formula (0.16277136F / friction³), potion effects, wandering AI, DataWatcher index 8 (packed RGB potion color), setHealth bug (aM not clamped), NBT

**Estimated effort:** ~6 hours equivalent
**Notes:** Item spec clarifies dual maxDamage paths — c() is the public API (overridden by tools), g()/a-field is only for h() (isDamageable predicate). LivingEntity setHealth bug preserved per quirk list.
