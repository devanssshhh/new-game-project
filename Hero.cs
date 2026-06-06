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
	private ProgressBar _healthBar; // FIXED: Added the HealthBar reference back
	private int _health;
	private bool _isAttacking;
	private bool _isShielding; // Tracks if the shield button is actively held down
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

		// FIXED: Find the HealthBar node over your head and set it to 100% on start
		_healthBar = GetNode<ProgressBar>("HealthBar");
		if (_healthBar != null)
		{
			_healthBar.MaxValue = MaxHealth;
			_healthBar.Value = MaxHealth;
		}
		else
		{
			GD.PrintErr("Error: Could not find a child node named 'HealthBar' under Hero!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_dead)
		{
			return;
		}

		Vector2 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Shield Input (Hold Mode)
		// Only allow shielding if the player is safely grounded and not actively swinging
		if (IsOnFloor() && !_isAttacking)
		{
			if (Input.IsActionPressed("shield_block"))
			{
				_isShielding = true;
			}
			else
			{
				_isShielding = false;
			}
		}
		else
		{
			_isShielding = false; // Automatically drop shield if falling/jumping
		}

		// Handle Jump (Disabled while holding shield)
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor() && !_isAttacking && !_isShielding)
		{
			velocity.Y = JumpVelocity;
		}

		Vector2 direction = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");

		// Combat Trigger Inputs (Attacks disabled while shielding)
		if (!_isAttacking && !_isShielding)
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

		// Handle Movement Speed
		if (!_isAttacking && !_isShielding)
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
			// Stops your character dead in their tracks while attacking or holding the shield up
			velocity.X = 0;
		}

		UpdateAnimation(direction.X);
		UpdateHitBoxFacing();

		Velocity = velocity;
		MoveAndSlide();
	}

	private void PlayAttack(string animation)
	{
		if (_isAttacking || _isShielding) return;

		_isAttacking = true;
		_sprite.Play(animation);
		_hitBox.StartAttack();
	}

	private void OnAnimationFinished()
	{
		// Clean up weapon attack animations
		if (_sprite.Animation == "lower-base-attack" || _sprite.Animation == "upper-base-attack")
		{
			_isAttacking = false;
			_hitBox.EndAttack();
		}
	}

	private void UpdateAnimation(float directionX)
	{
		// Let weapon attack animations finish completely uninterrupted
		if (_isAttacking)
		{
			return;
		}

		// Flip the sprite texture layout based on movement direction
		if (directionX > 0)
		{
			_sprite.FlipH = false;
		}
		else if (directionX < 0)
		{
			_sprite.FlipH = true;
		}

		// Determine which base animation state should be executing
		if (_isShielding)
		{
			// If we aren't already on the shield animation, start it.
			// Because looping is turned off in your editor, Godot will naturally 
			// run the frames to the end and freeze on the last frame automatically.
			if (_sprite.Animation != "shield")
			{
				_sprite.Play("shield");
			}
		}
		else
		{
			// Only process movement states if we are NOT blocking
			string animation = !IsOnFloor() ? "jump" : directionX != 0 ? "Walking" : "Idle";
			if (_sprite.Animation != animation)
			{
				_sprite.Play(animation);
			}
		}
	}

	// Mirror the hitbox to the side the sprite is facing.
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

		// Takes substantially reduced damage if hit while holding up the shield!
		if (_isShielding)
		{
			GD.Print("Blocked! Damage reduced.");
			damage = (int)(damage * 0.1f); // 90% damage reduction
		}

		_health -= damage;

		// FIXED: Update the overhead visual health bar value
		if (_healthBar != null)
		{
			_healthBar.Value = _health;
		}

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
		_isShielding = false;
		_hitBox.EndAttack();
		_sprite.Modulate = new Color(0.4f, 0.4f, 0.4f);
		Velocity = Vector2.Zero;
		_sprite.Play("Idle");

		// FIXED: Force bar to empty visual state on death
		if (_healthBar != null)
		{
			_healthBar.Value = 0;
		}

		SetPhysicsProcess(false);
	}
}
