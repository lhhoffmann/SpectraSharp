namespace SpectraSharp.ModTranspiler.Model;

sealed class EntityDescriptor
{
    public string ClassName    { get; set; } = "";
    public string SuperClass   { get; set; } = "";
    public float  MaxHealth    { get; set; } = 20f;
    public float  MoveSpeed    { get; set; } = 0.3f;
    public List<MethodBody> Methods { get; } = [];
}
