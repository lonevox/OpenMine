using OpenMine;
using Godot;

public partial class Entrypoint : Node
{
    public override async void _Ready()
    {
        await Game.Launch(GetTree());
    }
}
