using Godot;

// A fall-death line that spans the whole level. It tracks the player
// horizontally (so it is effectively infinite) and sits below the playfield.
// Falling into it kills the player and restarts the game.
public partial class KillZone : Area2D
{
	private Node2D _player;

	public override void _Ready()
	{
		Monitoring = true;
		BodyEntered += OnBodyEntered;
	}

	public override void _Process(double delta)
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		}

		if (_player != null)
		{
			// Follow the player horizontally; keep our (low) Y.
			GlobalPosition = new Vector2(_player.GlobalPosition.X, GlobalPosition.Y);
		}
	}

	private void OnBodyEntered(Node2D body)
	{
		if (body is Hero hero)
		{
			hero.Kill();
		}
	}
}
