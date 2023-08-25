using Godot;
using OpenMine.Registry;

public partial class Singleton : Resource, IRegistryItem
{
    [Export]
    public Node Node;

    public Singleton(Node node)
    {
        Node = node;
    }

    public void OnRegister()
    {

    }
}
