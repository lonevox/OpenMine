using Godot;
using OpenMine.Registry;

namespace OpenMine.Modding;

public abstract partial class Mod : Resource, IRegistryItem
{
    public ModMetadata Metadata { get; internal set; }

    public abstract void OnRegister();

    public abstract void PostRegister(GameSceneTree gameSceneTree);

    public static Identifier Id<T>(string path)
        where T : Mod
    {
        return Identifier.of(typeof(T), path);
    }
}
