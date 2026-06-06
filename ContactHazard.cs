using Godot;
using System.Collections.Generic;

public partial class ContactHazard : Area2D
{
	[Export] public int Damage = 20;
	[Export] public double DamageInterval = 0.8; // Deal damage every 0.8 seconds of contact

	private readonly Dictionary<HurtBox, double> _overlappingHurtBoxes = new();

	public override void _Ready()
	{
		AreaEntered += OnAreaEntered;
		AreaExited += OnAreaExited;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_overlappingHurtBoxes.Count == 0)
		{
			return;
		}

		// Use a list to store keys to avoid modification during iteration
		var keys = new List<HurtBox>(_overlappingHurtBoxes.Keys);
		foreach (var hurtBox in keys)
		{
			if (!IsInstanceValid(hurtBox))
			{
				_overlappingHurtBoxes.Remove(hurtBox);
				continue;
			}

			double timer = _overlappingHurtBoxes[hurtBox] - delta;
			if (timer <= 0)
			{
				hurtBox.TakeHit(Damage);
				timer = DamageInterval;
			}
			_overlappingHurtBoxes[hurtBox] = timer;
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		if (area is HurtBox hurtBox)
		{
			hurtBox.TakeHit(Damage);
			_overlappingHurtBoxes[hurtBox] = DamageInterval;
		}
	}

	private void OnAreaExited(Area2D area)
	{
		if (area is HurtBox hurtBox)
		{
			_overlappingHurtBoxes.Remove(hurtBox);
		}
	}
}
