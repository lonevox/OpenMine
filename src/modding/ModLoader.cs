using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace OpenMine.Modding;

public static class ModLoader
{
    private static readonly Dictionary<string, Assembly> AssemblyCache = new Dictionary<string, Assembly>();

    private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Load(Mod mod)
    {
        GD.Print("Loading mod assembly '" + mod.GetType().Name + "'...");

        var modAssembly = Assembly.GetAssembly(mod.GetType());
        var res = modAssembly.GetManifestResourceNames();

        // Load mod.json
        var modJsonResource = modAssembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("resources.mod.json"));
        // Deserialize mod.json
        ModMetadata modMetadata;
        using (Stream stream = modAssembly.GetManifestResourceStream(modJsonResource))
        using (StreamReader reader = new StreamReader(stream))
        {
            var json = reader.ReadToEnd();
            modMetadata = JsonSerializer.Deserialize<ModMetadata>(json, serializerOptions);
        }
        mod.Metadata = modMetadata;

        // Cache for use in LoadScene
        AssemblyCache.Add(modMetadata.Name, modAssembly);

        // Load any library dependencies packaged within the assembly
        AssemblyResolver.EmbeddedLibraryHook(modAssembly);

        // Open mod pck
        var pckResource = modAssembly.GetManifestResourceNames()
            .Single(str => str.EndsWith("resources.pck"));
        var pckPath = Path.Combine(Game.GlobalResourcesPath, ".OpenMine/pck");
        Directory.CreateDirectory(pckPath);
        using (var stream = modAssembly.GetManifestResourceStream(pckResource))
        {
            var modPckPath = Path.Combine(pckPath, "Core.pck");
            using (var fs = File.Create(modPckPath))
            {
                stream.CopyTo(fs);
            }
            ProjectSettings.LoadResourcePack(modPckPath);
        }

        GD.Print("Finished loading mod assembly '" + mod.Metadata.Id + "'");
    }

    public static Mod Load(Assembly modAssembly)
    {
        var modType = modAssembly.GetTypes()
            .Single(t => t.BaseType == typeof(Mod));
        var mod = Activator.CreateInstance(modType) as Mod;
        Load(mod);
        return mod;
    }

    public static HashSet<Mod> LoadAllInDirectory(DirectoryInfo modDirectory)
    {
        var modSet = new HashSet<Mod>();
        foreach (var file in modDirectory.EnumerateFiles())
        {
            if (!file.Extension.Equals(".dll"))
                continue;

            // Load mod
            var assembly = Assembly.LoadFrom(file.FullName);
            var mod = Load(assembly);
            modSet.Add(mod);
        }
        return modSet;
    }

    private static PackedScene PatchScene(PackedScene scene)
    {
        var bundled = scene._Bundled;
        var vars = (Godot.Collections.Array)bundled["variants"];
        for (var i = 0; i < vars.Count; i++)
        {
            var resource = (Resource)vars[i];
            if (resource == null)
                continue;
            if (resource is PackedScene innerScene)
                PatchScene(innerScene);
            else if (resource is CSharpScript script)
            {
                //Try to find the path of the script by the namespace and the resource itself
                var path = script.ResourcePath.Split("res://src/resources/")[1];

                // Find the class type based on the source file
                // NOTE: Determining class name via file name is possible due to a limitation in Godot: https://github.com/godotengine/godot/issues/15661
                var className = path.Split("/").Last().Replace(".cs", "");

                // Get the script's type name
                var ns = GetNamespaceOfSourceFile(script.ResourcePath);
                var type = ns + "." + className;

                // Get the script's assembly
                var mod = path.Split("/")[0];
                var assembly = AssemblyCache[mod];

                // Instantiate that script
                var thing = assembly.GetType(type).GetConstructor(Type.EmptyTypes)?.Invoke(Array.Empty<object>()) as GodotObject;
                var newScript = (CSharpScript)thing.GetScript();
                newScript.TakeOverPath(script.ResourcePath);
                newScript.SourceCode = script.SourceCode;

                //Change the script of the packed scene
                vars[i] = newScript;
                thing.Free();
            }
        }

        bundled["variants"] = vars;

        scene._Bundled = bundled;
        return scene;
    }

    public static PackedScene LoadScene(string scenePath)
    {
        var scene = GD.Load<PackedScene>(scenePath);
        return PatchScene(scene);
    }

    private static string GetNamespaceOfSourceFile(string path)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        var code = file.GetAsText();

        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetCompilationUnitRoot();

        var namespaceDeclaration = root.DescendantNodes()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();
        if (namespaceDeclaration != null)
            return namespaceDeclaration.Name.ToString();

        namespaceDeclaration = root.DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();
        if (namespaceDeclaration != null)
            return namespaceDeclaration.Name.ToString();

        return "";
    }
}
