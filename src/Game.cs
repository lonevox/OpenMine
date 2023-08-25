using System.IO;
using System.Threading.Tasks;
using Godot;
using OpenMine.Modding;
using OpenMine.Registry;

namespace OpenMine;

public class Game
{
    public static readonly ModRegistry ModRegistry = new ModRegistry();
    public static readonly RegistryCollection Registries = new RegistryCollection();
    private static GameStage _stage = GameStage.PreLaunch;
    public static GameStage Stage
    {
        get => _stage;
        private set
        {
            _stage = value;
            GD.Print("\n------ Game Stage: " + _stage + " ------");
        }
    }
    public static readonly string ResourcesPath = "res://";
    public static readonly string UserPath = "user://";
    public static readonly string ModResourcesPath = Path.Combine(ResourcesPath, "src/resources");
    public static readonly string GlobalResourcesPath = ProjectSettings.GlobalizePath("res://");
    public static readonly string GlobalUserPath = ProjectSettings.GlobalizePath("user://");
    public static readonly string GlobalModResourcesPath = Path.Combine(GlobalResourcesPath, "src/resources");

    private static GameSceneTree gameSceneTree;

    public enum GameStage
    {
        PreLaunch,
        Launch,
        PostLaunch,
    }

    public static async Task Launch(SceneTree sceneTree)
    {
        GD.Print("Starting OpenMine...");
        Stage = GameStage.Launch;

        // Create reference to scene tree.
        gameSceneTree = new GameSceneTree(sceneTree);

        // Create user folders if they don't exist.
        var worldsDirectory = Directory.CreateDirectory(GlobalUserPath + "/worlds");
        var modsDirectory = Directory.CreateDirectory(GlobalUserPath + "/mods");

        // Await process frame so that the scene tree finishes loading.
        await gameSceneTree.SceneTree.ToSignal(gameSceneTree.SceneTree, "process_frame");

        // Add singleton registry.
        Registries.Add(new Registry<Singleton>());
        Registries.Get<Singleton>().OnRegister += singleton => gameSceneTree.AddSingleton(singleton);

        // Set up assembly resolution for mods that depend on each other.
        var path = Path.Combine(OS.GetUserDataDir(), "mods");
        AssemblyResolver.Hook(path);

        // Load and register mods that were attempted to be registered before launch.
        foreach (var mod in ModRegistry.PreLaunchMods)
        {
            ModLoader.Load(mod);
            ModRegistry.Register(mod);
        }

        // Load and register all mods in the mods folder.
        foreach (var mod in ModLoader.LoadAllInDirectory(modsDirectory))
        {
            ModRegistry.Register(mod);
        }

        // Post mod registry.
        foreach (var mod in ModRegistry.GetAll())
        {
            GD.Print("--- Post-registry for " + mod.GetType() + " ---");
            mod.PostRegister(gameSceneTree);
        }
        GD.Print("Mod registration finished");
        Stage = GameStage.PostLaunch;
    }

    public static string Resource<T>()
        where T : Mod
    {
        return ModResourcesPath + "/" + typeof(T).Name;
    }

    public static string Resource<T>(string path)
        where T : Mod
    {
        return Resource<T>() + "/" + path;
    }
}

public class GameSceneTree
{
    public readonly SceneTree SceneTree;

    internal GameSceneTree(SceneTree sceneTree)
    {
        SceneTree = sceneTree;
    }

    public Game GameInstance() => SceneTree.Root.GetNode<Game>("Game");

    /// <summary>
    /// Adds a node as a singleton to the Game's scene tree, in much the same way that you can do in the Godot editor.
    /// Useful for adding global behavior.
    /// </summary>
    /// <param name="node"></param>
    internal void AddSingleton(Singleton singleton)
    {
        SceneTree.Root.AddChild(singleton.Node);
    }

    public Node GetSingleton(Singleton singleton)
    {
        return SceneTree.Root.GetNode(singleton.Node.GetPath());
    }
}
