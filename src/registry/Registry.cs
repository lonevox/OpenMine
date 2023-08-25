using System;
using System.Collections.Generic;
using Godot;

namespace OpenMine.Registry;

public interface Registry
{
    Type Type { get; }
}

/// <summary>
/// Registry allows you to 
/// </summary>
/// <remarks>If you're a modder, you likely don't want to use this type, and should instead use <see cref="Registry<T></remarks>
/// <typeparam name="I"></typeparam>
/// <typeparam name="T"></typeparam>
public class Registry<I, T> : Registry
    where T : Resource, IRegistryItem
{
    protected Dictionary<I, T> values = new Dictionary<I, T>();
    protected Dictionary<T, I> valuesReversed = new Dictionary<T, I>();
    public event Action<T> OnRegister = (t) => { };

    public Type Type => typeof(T);

    public T Get(I id)
    {
        try
        {
            return values[id];
        }
        catch (KeyNotFoundException e)
        {
            throw new NotInRegistryException("The ID '" + id + "' was not found in the registry.", e);
        }
    }

    public I IdentifierOf(T item)
    {
        try
        {
            return valuesReversed[item];
        }
        catch (KeyNotFoundException e)
        {
            throw new NotInRegistryException("The item '" + item + "' was not found in the registry.", e);
        }
    }

    public Dictionary<I, T>.Enumerator GetEnumerator()
    {
        return values.GetEnumerator();
    }

    public Dictionary<I, T>.ValueCollection GetAll()
    {
        return values.Values;
    }

    public bool Has(I id)
    {
        return values.ContainsKey(id);
    }

    public T Register(I id, T item)
    {
        if (values.ContainsKey(id))
        {
            throw new ArgumentException(paramName: nameof(id), message: "Item with ID '" + id + "' already exists in registry.");
        }
        values[id] = item;
        valuesReversed[item] = id;
        item.OnRegister();
        OnRegister.Invoke(item);
        return item;
    }
}

public class Registry<T> : Registry<Identifier, T>
    where T : Resource, IRegistryItem
{
    protected Dictionary<Type, HashSet<T>> valuesByMod = new Dictionary<Type, HashSet<T>>();

    public HashSet<T> GetAllFromMod(Type modType)
    {
        return valuesByMod[modType];
    }

    public new T Register(Identifier id, T item)
    {
        base.Register(id, item);
        if (valuesByMod.ContainsKey(id.ModType))
        {
            valuesByMod[id.ModType].Add(item);
        }
        else
        {
            valuesByMod.Add(id.ModType, new HashSet<T>());
        }
        GD.Print(item.GetType() + " registered: " + id);
        return item;
    }
}

public class NotInRegistryException : Exception
{
    public NotInRegistryException() { }

    public NotInRegistryException(string message) : base(message) { }

    public NotInRegistryException(string message, Exception inner) : base(message, inner) { }
}
