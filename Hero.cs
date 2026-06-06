using Godot;
using System;

public partial class Hero : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	private AnimatedSprite2D _sprite;
	private bool _isAttacking;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Hero");
		_sprite.AnimationFinished += OnAnimationFinished;
	}

	public override void _PhysicsProcess(double delta)
	{
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
	}

	private void OnAnimationFinished()
	{
		if (_sprite.Animation == "lower-base-attack" || _sprite.Animation == "upper-base-attack")
		{
			_isAttacking = false;
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
}
