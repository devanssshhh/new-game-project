using Godot;

public partial class EnemySamurai : CharacterBody2D
{
	[Export] public float Speed = 120.0f;
	[Export] public float DetectRange = 350.0f;
	[Export] public float AttackRange = 90.0f;
	[Export] public float AttackCooldown = 1.2f;
	[Export] public int MaxHealth = 60;

	private AnimatedSprite2D _sprite;
	private HitBox _hitBox;
	private Vector2 _hitBoxBaseOffset;
	private Node2D _player;
	private int _health;
	private bool _attacking;
	private bool _dead;
	private double _cooldown;

	public override void _Ready()
	{
		AddToGroup("enemy");
		_health = MaxHealth;

		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_sprite.AnimationFinished += OnAnimationFinished;

		_hitBox = GetNode<HitBox>("HitBox");
		_hitBoxBaseOffset = _hitBox.Position;

		GetNode<HurtBox>("HurtBox").Hurt += OnHurt;
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

		_cooldown -= delta;

		Node2D player = GetPlayer();
		if (_attacking || player == null)
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
		}
		else
		{
			float dx = player.GlobalPosition.X - GlobalPosition.X;
			float distance = Mathf.Abs(dx);

			// Face the hero. The samurai art faces right by default, so flip when
			// the hero is to the left.
			_sprite.FlipH = dx < 0;
			UpdateHitBoxFacing();

			if (distance <= AttackRange && _cooldown <= 0)
			{
				Attack();
				velocity.X = 0;
			}
			else if (distance <= DetectRange && distance > AttackRange)
			{
				velocity.X = Mathf.Sign(dx) * Speed;
				PlayLoop("Run");
			}
			else
			{
				velocity.X = Mathf.MoveToward(velocity.X, 0, Speed);
				PlayLoop("Walk");
			}
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private Node2D GetPlayer()
	{
		if (_player == null || !IsInstanceValid(_player))
		{
			_player = GetTree().GetFirstNodeInGroup("player") as Node2D;
		}

		return _player;
	}

	private void Attack()
	{
		_attacking = true;
		_cooldown = AttackCooldown;
		_sprite.Play("attack standing");
		_hitBox.StartAttack();
	}

	private void PlayLoop(string animation)
	{
		if (_sprite.Animation != animation)
		{
			_sprite.Play(animation);
		}
	}

	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "attack standing")
		{
			_attacking = false;
			_hitBox.EndAttack();
		}
	}

	// Mirror the hitbox to the side the samurai is facing.
	private void UpdateHitBoxFacing()
	{
		float x = Mathf.Abs(_hitBoxBaseOffset.X) * (_sprite.FlipH ? -1.0f : 1.0f);
		_hitBox.Position = new Vector2(x, _hitBoxBaseOffset.Y);
	}

	private void OnHurt(int damage)
	{
		if (_dead)
		{
			return;
		}

		_health -= damage;
		GD.Print($"Samurai took {damage} damage! Current health: {_health}");
		Flash();

		if (_health <= 0)
		{
			GD.Print("Samurai died!");
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
		_attacking = false;
		_hitBox.EndAttack();
		_sprite.Modulate = new Color(0.4f, 0.4f, 0.4f);
		Velocity = Vector2.Zero;
		SetPhysicsProcess(false);

		// Remove the enemy shortly after dying.
		SceneTreeTimer timer = GetTree().CreateTimer(0.6);
		timer.Timeout += QueueFree;
	}
}
