using System;
using System.Collections.Generic;
using Godot;

namespace OpenMine.Registry;

/// <summary>
/// Identifiers consist of a mod and a unique path. The mod must be unique.
/// </summary>
public class Identifier
{
    public readonly Type ModType;
    public readonly StringName Path;

    private static readonly Dictionary<(Type ModType, string Path), Identifier> idCache = new Dictionary<(Type ModType, string Path), Identifier>();

    private Identifier(Type mod, string path)
    {
        if (!Game.ModRegistry.Has(mod))
            throw new NotInRegistryException("The mod '" + mod + "' has not been registered.");
        ModType = mod;
        Path = path;
    }

    // TODO: I have no idea if this caching actually helps or makes it worse. All I know is Identifiers are going to be needed a lot, so this might help with memory allocation rate?
    public static Identifier of(Type mod, string path)
    {
        Identifier id;
        if (idCache.TryGetValue((mod, path), out id))
            return id;
        id = new Identifier(mod, path);
        idCache.Add((mod, path), id);
        return id;
    }

    public override string ToString()
    {
        return ModType + ":" + Path;
    }

    public override bool Equals(object obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;
        return ModType.Equals(obj) && Path.Equals(((Identifier)obj).Path);
    }

    public override int GetHashCode()
    {
        return Tuple.Create(ModType, Path).GetHashCode();
    }
}
