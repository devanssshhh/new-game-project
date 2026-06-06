using Godot;

public partial class EnemyGoblin : CharacterBody2D
{
	[Export] public float Speed = 30.0f;
	[Export] public int MaxHealth = 10;
	[Export] public float ProximityRange = 60.0f;
	[Export] public float DetectRange = 300.0f;
	[Export] public float PatrolRange = 150.0f;

	private AnimatedSprite2D _sprite;
	private Node2D _player;
	private int _health;
	private bool _dead;
	private float _startX;
	private int _direction = 1; // 1 = right, -1 = left
	private int _prevPlayerSide = 0;

	public override void _Ready()
	{
		AddToGroup("enemy");
		_health = MaxHealth;
		_startX = GlobalPosition.X;

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		GetNode<HurtBox>("HurtBox").Hurt += OnHurt;
	}

	private Node2D GetPlayer()
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		}
		return _player;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_dead)
		{
			return;
		}

		Vector2 velocity = Velocity;

		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		Node2D player = GetPlayer();
		bool inProximity = false;
		bool inDetectRange = false;

		float leftLimit = _startX - PatrolRange;
		float rightLimit = _startX + PatrolRange;

		if (player != null)
		{
			float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
			if (distance <= DetectRange)
			{
				inDetectRange = true;
			}
			if (distance <= ProximityRange)
			{
				inProximity = true;
			}
		}

		bool targetPlayer = false;
		if (player != null && inDetectRange)
		{
			// Only target/follow the player's position if they are within the patrol bounds
			if (player.GlobalPosition.X >= leftLimit && player.GlobalPosition.X <= rightLimit)
			{
				targetPlayer = true;
			}
		}

		if (targetPlayer && player != null)
		{
			float dx = player.GlobalPosition.X - GlobalPosition.X;
			int currentSide = dx > 0 ? 1 : -1;

			if (inProximity)
			{
				// Force turn towards player when they are in close proximity
				_direction = currentSide;
				_prevPlayerSide = currentSide;
			}
			else
			{
				// Target the player (turn on crossovers/jump-overs)
				if (_prevPlayerSide == 0)
				{
					_direction = currentSide;
				}
				else if (currentSide != _prevPlayerSide)
				{
					_direction = currentSide;
				}
				_prevPlayerSide = currentSide;
			}
		}
		else
		{
			_prevPlayerSide = 0;
		}

		// Boundary limits check (always active so it stays within range back-and-forth)
		if (_direction > 0 && GlobalPosition.X >= rightLimit)
		{
			_direction = -1;
		}
		else if (_direction < 0 && GlobalPosition.X <= leftLimit)
		{
			_direction = 1;
		}

		velocity.X = _direction * Speed;
		_sprite.FlipH = _direction < 0;

		if (inDetectRange && inProximity)
		{
			if (!_sprite.IsPlaying() || _sprite.Animation != "Attack")
			{
				_sprite.Play("Attack");
			}
		}
		else
		{
			if (!_sprite.IsPlaying() || _sprite.Animation != "Walk")
			{
				_sprite.Play("Walk");
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void OnHurt(int damage)
	{
		if (_dead)
		{
			return;
		}

		_health -= damage;
		GD.Print($"Goblin took {damage} damage! Current health: {_health}");
		Flash();

		if (_health <= 0)
		{
			GD.Print("Goblin died!");
			Die();
		}
	}

	private void Flash()
	{
		_sprite.Modulate = new Color(1f, 0.3f, 0.3f);
		Tween tween = CreateTween();
		tween.TweenProperty(_sprite, "modulate", Colors.White, 0.2f);
	}

	private void Die()
	{
		_dead = true;
		_sprite.Modulate = new Color(0.4f, 0.4f, 0.4f);
		Velocity = Vector2.Zero;
		SetPhysicsProcess(false);

		// Disable hurtbox and contact hazard so they can't be interacted with anymore.
		GetNode<Area2D>("HurtBox").SetDeferred(Area2D.PropertyName.Monitorable, false);
		GetNode<Area2D>("ContactHazard").SetDeferred(Area2D.PropertyName.Monitoring, false);

		// Remove the enemy shortly after dying.
		SceneTreeTimer timer = GetTree().CreateTimer(0.6);
		timer.Timeout += QueueFree;
	}
}
