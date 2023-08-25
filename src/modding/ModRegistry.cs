using System;
using System.Collections.Generic;
using Godot;
using OpenMine.Registry;

namespace OpenMine.Modding;

public class ModRegistry : Registry<Type, Mod>
{
    public readonly HashSet<Mod> PreLaunchMods = new HashSet<Mod>();

    public new Mod Register(Type modType, Mod mod)
    {
        if (!modType.BaseType.Equals(typeof(Mod)))
            throw new ArgumentException(paramName: nameof(modType), message: "modType extends '" + modType.BaseType + "' when it must extend Mod.");

        // Delay registration if Register() was called before Game.Launch().
        if (Game.Stage == Game.GameStage.PreLaunch)
        {
            PreLaunchMods.Add(mod);
            return mod;
        }

        GD.Print("--- Registry for mod '" + modType + "' ---");
        base.Register(modType, mod);
        return mod;
    }

    public Mod Register(Mod mod)
    {
        // Delay registration if Register() was called before Game.Launch().
        if (Game.Stage == Game.GameStage.PreLaunch)
        {
            PreLaunchMods.Add(mod);
            return mod;
        }

        GD.Print("--- Registry for mod '" + mod.GetType() + "' ---");
        return base.Register(mod.GetType(), mod);
    }
}
