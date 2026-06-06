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
		GD.Print($"{Owner?.Name}'s HitBox starting attack");
		_hitThisSwing.Clear();
		SetDeferred(Area2D.PropertyName.Monitoring, true);
	}

	public void EndAttack()
	{
		GD.Print($"{Owner?.Name}'s HitBox ending attack");
		SetDeferred(Area2D.PropertyName.Monitoring, false);
	}

	private void OnAreaEntered(Area2D area)
	{
		GD.Print($"{Owner?.Name}'s HitBox entered {area.Owner?.Name}'s {area.Name}");
		// _hitThisSwing.Add returns false if the target was already hit this swing.
		if (area is HurtBox hurtBox && _hitThisSwing.Add(hurtBox))
		{
			GD.Print($"{Owner?.Name}'s HitBox dealing {Damage} damage to {area.Owner?.Name}");
			hurtBox.TakeHit(Damage);
		}
	}
}
