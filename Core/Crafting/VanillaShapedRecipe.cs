namespace SpectraEngine.Core.Crafting;

/// <summary>
/// Replica of <c>aga</c> (ShapedRecipes) — a shaped crafting recipe that matches
/// a fixed 2D pattern of ingredients anywhere within the crafting grid.
///
/// Matching checks both normal and horizontally-mirrored orientations (spec §1.3).
///
/// Source spec: Documentation/VoxelCore/Parity/Specs/CraftingRecipes_Spec.md §1.3
/// </summary>
public sealed class VanillaShapedRecipe : ICraftingRecipe
{
    private readonly CraftingIngredient[] _pattern; // row-major, [row * Width + col]
    private readonly ItemStack            _result;

    public readonly int PatternWidth;
    public readonly int PatternHeight;

    // ── Constructor ───────────────────────────────────────────────────────────

    /// <param name="patternWidth">Width of the recipe pattern (1–3).</param>
    /// <param name="patternHeight">Height of the recipe pattern (1–3).</param>
    /// <param name="pattern">
    /// Row-major ingredient array, length = width × height.
    /// Use <see cref="CraftingIngredient.Empty"/> for empty cells.
    /// </param>
    /// <param name="result">Output stack.</param>
    public VanillaShapedRecipe(int patternWidth, int patternHeight,
                               CraftingIngredient[] pattern, ItemStack result)
    {
        PatternWidth  = patternWidth;
        PatternHeight = patternHeight;
        _pattern      = pattern;
        _result       = result;
    }

    // ── ICraftingRecipe ───────────────────────────────────────────────────────

    public ItemStack GetResult() => _result.Copy();

    public bool Matches(CraftingGrid grid)
    {
        // Try all valid top-left offsets for both normal and mirrored orientations.
        for (int dy = 0; dy <= grid.Height - PatternHeight; dy++)
        {
            for (int dx = 0; dx <= grid.Width - PatternWidth; dx++)
            {
                if (MatchesAt(grid, dx, dy, mirror: false)) return true;
                if (MatchesAt(grid, dx, dy, mirror: true))  return true;
            }
        }
        return false;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if the grid, when viewed with the recipe pattern starting at
    /// (offsetX, offsetY), satisfies all ingredient requirements.
    /// Cells outside the recipe bounds must be empty.
    /// </summary>
    private bool MatchesAt(CraftingGrid grid, int offsetX, int offsetY, bool mirror)
    {
        for (int gy = 0; gy < grid.Height; gy++)
        {
            for (int gx = 0; gx < grid.Width; gx++)
            {
                int rx = gx - offsetX; // position within recipe pattern
                int ry = gy - offsetY;

                CraftingIngredient required;
                if (rx < 0 || rx >= PatternWidth || ry < 0 || ry >= PatternHeight)
                {
                    // Outside recipe bounds — grid cell must be empty
                    required = CraftingIngredient.Empty;
                }
                else
                {
                    int px = mirror ? (PatternWidth - 1 - rx) : rx;
                    required = _pattern[ry * PatternWidth + px];
                }

                if (!required.Matches(grid.GetSlot(gx, gy))) return false;
            }
        }
        return true;
    }
}
