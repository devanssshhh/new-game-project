using Godot;
using System.Collections.Generic;

// Area2D that represents an attack's reach. It is disabled by default and only
// turned on for the duration of a swing (StartAttack / EndAttack). While active
// it deals damage to any HurtBox it overlaps, once per swing per target.
public partial class HitBox : Area2D
{
	[Export] public int Damage = 10;

	private readonly HashSet<HurtBox> _hitThisSwing = new();

	public override void _Ready()
	{
		// A hitbox is the detector, not the detected.
		Monitorable = false;
		Monitoring = false;
		AreaEntered += OnAreaEntered;
	}

	// Enable the hitbox for a single attack swing. SetDeferred is required
	// because attacks are triggered from _PhysicsProcess, and monitoring state
	// cannot be changed while the physics server is flushing queries.
	public void StartAttack()
	{
		_hitThisSwing.Clear();
		SetDeferred(Area2D.PropertyName.Monitoring, true);
	}

	public void EndAttack()
	{
		SetDeferred(Area2D.PropertyName.Monitoring, false);
	}

	private void OnAreaEntered(Area2D area)
	{
		// _hitThisSwing.Add returns false if the target was already hit this swing.
		if (area is HurtBox hurtBox && _hitThisSwing.Add(hurtBox))
		{
			hurtBox.TakeHit(Damage);
		}
	}
}
