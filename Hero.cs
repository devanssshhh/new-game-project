using Godot;
using System;

public partial class Hero : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	[Export] public int MaxHealth = 100;

	private AnimatedSprite2D _sprite;
	private HitBox _hitBox;
	private Vector2 _hitBoxBaseOffset;
	private int _health;
	private bool _isAttacking;
	private bool _dead;

	public override void _Ready()
	{
		AddToGroup("player");
		_health = MaxHealth;

		_sprite = GetNode<AnimatedSprite2D>("Hero");
		_sprite.AnimationFinished += OnAnimationFinished;

		_hitBox = GetNode<HitBox>("HitBox");
		_hitBoxBaseOffset = _hitBox.Position;

		GetNode<HurtBox>("HurtZone").Hurt += OnHurt;
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

		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		if (!_isAttacking)
		{
			if (Input.IsActionJustPressed("lower-base-attack"))
			{
				PlayAttack("lower-base-attack");
			}
			else if (Input.IsActionJustPressed("upper-base-attack"))
			{
				PlayAttack("upper-base-attack");
			}
		}

		if (!_isAttacking)
		{
			if (direction != Vector2.Zero)
			{
				velocity.X = direction.X * Speed;
			}
			else
			{
				velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			}
		}
		else
		{
			velocity.X = 0;
		}

		UpdateAnimation(direction.X);
		UpdateHitBoxFacing();

		Velocity = velocity;
		MoveAndSlide();
	}

	private void PlayAttack(string animation)
	{
		if (_isAttacking)
		{
			return;
		}

		_isAttacking = true;
		_sprite.Play(animation);
		_hitBox.StartAttack();
	}

	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "lower-base-attack" || _sprite.Animation == "upper-base-attack")
		{
			_isAttacking = false;
			_hitBox.EndAttack();
		}
	}

	private void UpdateAnimation(float directionX)
	{
		if (_isAttacking)
		{
			return;
		}

		if (directionX > 0)
		{
			_sprite.FlipH = false;
		}
		else if (directionX < 0)
		{
			_sprite.FlipH = true;
		}

		string animation = !IsOnFloor() ? "jump" : directionX != 0 ? "Walking" : "Idle";
		if (_sprite.Animation != animation)
		{
			_sprite.Play(animation);
		}
	}

	// Mirror the hitbox to the side the sprite is facing. FlipH == false means
	// facing right (positive X), FlipH == true means facing left.
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
		Flash();

		if (_health <= 0)
		{
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
		_isAttacking = false;
		_hitBox.EndAttack();
		_sprite.Modulate = new Color(0.4f, 0.4f, 0.4f);
		Velocity = Vector2.Zero;
		_sprite.Play("Idle");
		SetPhysicsProcess(false);
	}
}
