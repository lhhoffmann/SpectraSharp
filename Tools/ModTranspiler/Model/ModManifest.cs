namespace SpectraEngine.ModTranspiler.Model;

/// <summary>
/// In-memory representation of everything a mod adds or changes.
/// Produced by ManifestBuilder (Phase 3), consumed by Translator (Phase 4).
/// No files are written until Phase 5.
/// </summary>
sealed class ModManifest
{
    public string ModName { get; init; } = "";

    public List<BlockDescriptor>      NewBlocks    { get; } = [];
    public List<ItemDescriptor>       NewItems     { get; } = [];
    public List<EntityDescriptor>     NewEntities  { get; } = [];
    public List<InjectionDescriptor>  Overrides    { get; } = [];
    public List<RecipeDescriptor>     NewRecipes   { get; } = [];
    public List<WorldGenDescriptor>   WorldGenHooks { get; } = [];

    public int TotalObjects =>
        NewBlocks.Count + NewItems.Count + NewEntities.Count +
        Overrides.Count + NewRecipes.Count + WorldGenHooks.Count;
}
