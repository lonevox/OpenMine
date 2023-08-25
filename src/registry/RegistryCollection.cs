using System;
using System.Collections.Generic;
using Godot;

namespace OpenMine.Registry;

public class RegistryCollection
{
    private Dictionary<Type, Registry> registries = new Dictionary<Type, Registry>();

    public void Add<T>(Registry<T> registry)
        where T : Resource, IRegistryItem
    {
        if (registries.ContainsKey(registry.Type))
        {
            throw new ArgumentException(paramName: nameof(registry), message: "Multiple registries of the same type aren't allowed.");
        }
        registries[registry.Type] = registry;
        GD.Print("Registry added: " + registry.Type);
    }

    public Registry<T> Get<T>()
        where T : Resource, IRegistryItem
    {
        return registries[typeof(T)] as Registry<T>;
    }
}
