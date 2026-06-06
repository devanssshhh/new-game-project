using Godot;

// Area2D that represents where a character can BE hit.
// A HitBox calls TakeHit() on it, which forwards the damage to the owner
// (the owning character connects to the Hurt signal).
public partial class HurtBox : Area2D
{
	[Signal]
	public delegate void HurtEventHandler(int damage);

	public override void _Ready()
	{
		// A hurtbox is only ever detected by hitboxes; it never detects on its own.
		Monitoring = false;
		Monitorable = true;
	}

	public void TakeHit(int damage)
	{
		EmitSignal(SignalName.Hurt, damage);
	}
}
