// Stub for net.minecraft.world.biome.Biome — Minecraft 1.16.5

namespace net.minecraft.world.biome;

/// <summary>
/// MinecraftStubs v1_16 — Biome.
/// Minimal stub; mod code rarely interrogates biome internals.
/// </summary>
public class Biome
{
    public virtual float  getTemperature()      => 0.5f;
    public virtual float  getRainfall()         => 0.5f;
    public virtual string getRegistryName()     => "minecraft:plains";
    public virtual string getJavaClassName()    => "net.minecraft.world.biome.Biome";
}
