using Godot;
using System;

public partial class Hero : CharacterBody2D
{
	public const float Speed = 300.0f;
	public const float JumpVelocity = -400.0f;

	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Hero");
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpVelocity;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		UpdateAnimation(direction.X);

		Velocity = velocity;
		MoveAndSlide();
	}

	private void UpdateAnimation(float directionX)
	{
		// The sprites face left by default, so flip horizontally only when
		// moving right. Keep the current facing while idle.
		if (directionX > 0)
		{
			_sprite.FlipH = false;
		}
		else if (directionX < 0)
		{
			_sprite.FlipH = true;
		}

		// Play "Walking" while there is horizontal input, otherwise "Idle".
		string animation = directionX != 0 ? "Walking" : "Idle";
		if (_sprite.Animation != animation)
		{
			_sprite.Play(animation);
		}
	}
}
