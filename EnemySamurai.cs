using Godot;

public partial class EnemySamurai : CharacterBody2D
{
    // Movement
    [Export] public float WalkSpeed    = 65.0f;
    [Export] public float RunSpeed     = 150.0f;
    [Export] public float DetectRange  = 400.0f;
    [Export] public float AttackRange  = 90.0f;
    [Export] public float JumpVelocity = -340.0f;

    // Health
    [Export] public int MaxHealth = 80;

    // Damage per attack type
    [Export] public int DamageStanding   = 10;
    [Export] public int DamageBackSlash  = 18;
    [Export] public int DamageThurst     = 14;

    // Difficulty knobs
    [Export] public float ShieldDuration     = 1.5f;   // seconds shield stays up
    [Export] public float ShieldDamageBlock  = 0.85f;  // fraction absorbed while shielding
    [Export] public float ShieldChanceOnHurt = 0.40f;  // chance to shield after taking a hit
    [Export] public float DodgeJumpChance    = 0.30f;  // chance to jump away when player enters range
    [Export] public float LeapChance         = 0.15f;  // chance to leap toward player while chasing

    // Animation names exactly as defined in EnemySumrai.tscn
    private const string AnimIdle        = "idle standing";
    private const string AnimWalk        = "Walk";
    private const string AnimRun         = "Run";
    private const string AnimAttack1     = "attack standing";
    private const string AnimAttack2     = "back slash attack";
    private const string AnimAttack3     = "thurst attack";
    private const string AnimJump        = "jump";
    private const string AnimShield      = "sheild";
    private const string AnimHurt        = "hurt";
    private const string AnimDead        = "dead";

    private enum State { Idle, Chase, Attack, Jump, Hurt, Shield, Dead }

    private AnimatedSprite2D _sprite;
    private HitBox _hitBox;
    private Vector2 _hitBoxBaseOffset;
    private Node2D _player;
    private int _health;
    private State _state = State.Idle;

    private double _attackCooldown;
    private double _attackDuration;  // counts down to end current attack
    private double _hurtStun;
    private double _shieldTimer;
    private double _jumpCooldown;

    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        AddToGroup("enemy");
        _health = MaxHealth;
        _rng.Randomize();

        _attackCooldown = _rng.RandfRange(0.8f, 2.0f);
        _jumpCooldown   = _rng.RandfRange(0.5f, 1.5f);  // short initial delay so dodges appear early

        _sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _sprite.AnimationFinished += OnAnimationFinished;

        _hitBox = GetNode<HitBox>("HitBox");
        _hitBoxBaseOffset = _hitBox.Position;

        // Callable.From is more reliable than += across Godot 4.x versions
        GetNode<HurtBox>("HurtBox").Connect(
            HurtBox.SignalName.Hurt, Callable.From<int>(OnHurt));
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_state == State.Dead) return;

        Vector2 velocity = Velocity;
        if (!IsOnFloor()) velocity += GetGravity() * (float)delta;

        _attackCooldown -= delta;
        _jumpCooldown   -= delta;

        Node2D player = GetPlayer();

        switch (_state)
        {
            case State.Idle:
                velocity.X = Mathf.MoveToward(velocity.X, 0, RunSpeed);
                PlayLoop(AnimIdle);
                if (player != null && GlobalPosition.DistanceTo(player.GlobalPosition) <= DetectRange)
                    _state = State.Chase;
                break;

            case State.Chase:
                velocity = HandleChase(velocity, player);
                break;

            case State.Attack:
                velocity.X = Mathf.MoveToward(velocity.X, 0, RunSpeed);
                _attackDuration -= delta;
                if (_attackDuration <= 0) EndAttack();
                break;

            case State.Hurt:
                velocity.X = Mathf.MoveToward(velocity.X, 0, RunSpeed);
                _hurtStun -= delta;
                if (_hurtStun <= 0) ExitHurt();
                break;

            case State.Shield:
                velocity.X = Mathf.MoveToward(velocity.X, 0, RunSpeed);
                PlayLoop(AnimShield);
                _shieldTimer -= delta;
                if (_shieldTimer <= 0) ExitShield();
                break;

            case State.Jump:
                // Let the initial impulse carry through — air friction only, no steering
                velocity.X = Mathf.MoveToward(velocity.X, 0, 30f);
                if (IsOnFloor() && velocity.Y >= 0) ExitJump();
                break;
        }

        // Always face player
        if (player != null)
        {
            float dx = player.GlobalPosition.X - GlobalPosition.X;
            _sprite.FlipH = dx < 0;
            UpdateHitBoxFacing();
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    // ── Chase ─────────────────────────────────────────────────────────────────

    private Vector2 HandleChase(Vector2 velocity, Node2D player)
    {
        if (player == null) { _state = State.Idle; return velocity; }

        float dx       = player.GlobalPosition.X - GlobalPosition.X;
        float distance = Mathf.Abs(dx);

        if (distance > DetectRange * 1.3f) { _state = State.Idle; return velocity; }

        // Dodge jump away when player steps inside attack range
        if (distance < AttackRange * 1.4f && _jumpCooldown <= 0 && IsOnFloor()
            && _rng.Randf() < DodgeJumpChance)
        {
            return DoJump(velocity, -Mathf.Sign(dx), 1.05f);
        }

        // Leap toward player while chasing
        if (distance > AttackRange * 2f && _jumpCooldown <= 0 && IsOnFloor()
            && _rng.Randf() < LeapChance)
        {
            return DoJump(velocity, Mathf.Sign(dx), 0.80f);
        }

        if (distance <= AttackRange && _attackCooldown <= 0)
        {
            StartAttack();
            velocity.X = 0;
        }
        else if (distance > AttackRange)
        {
            bool running = distance > DetectRange * 0.5f;
            velocity.X = Mathf.Sign(dx) * (running ? RunSpeed : WalkSpeed);
            PlayLoop(running ? AnimRun : AnimWalk);
        }
        else
        {
            // In range but cooldown not ready — hold stance
            velocity.X = Mathf.MoveToward(velocity.X, 0, RunSpeed);
            PlayLoop(AnimIdle);
        }

        return velocity;
    }

    // ── Attacks ───────────────────────────────────────────────────────────────

    private void StartAttack()
    {
        _state = State.Attack;

        // Pick attack randomly — each has different damage, speed and cooldown
        int pick = _rng.RandiRange(0, 2);
        string anim;

        switch (pick)
        {
            case 0:
                anim             = AnimAttack1;
                _hitBox.Damage   = DamageStanding;
                _attackDuration  = 1.5f;  // 6 frames @ speed 4
                _attackCooldown  = _rng.RandfRange(1.0f, 1.8f);
                break;
            case 1:
                anim             = AnimAttack2;
                _hitBox.Damage   = DamageBackSlash;
                _attackDuration  = 1.0f;  // 4 frames @ speed 4
                _attackCooldown  = _rng.RandfRange(2.2f, 3.2f);
                break;
            default:
                anim             = AnimAttack3;
                _hitBox.Damage   = DamageThurst;
                _attackDuration  = 1.0f;  // 3 frames @ speed 3
                _attackCooldown  = _rng.RandfRange(1.4f, 2.4f);
                break;
        }

        _sprite.Play(anim);
        _hitBox.StartAttack();
    }

    private void EndAttack()
    {
        _hitBox.EndAttack();
        _state = State.Chase;
    }

    // ── Jumping ───────────────────────────────────────────────────────────────

    private Vector2 DoJump(Vector2 vel, float dirX, float strengthMult)
    {
        _state        = State.Jump;
        _jumpCooldown = _rng.RandfRange(2.5f, 4.5f);

        vel.Y = JumpVelocity * strengthMult;
        vel.X = dirX * RunSpeed * 1.4f;

        _sprite.Play(AnimJump);
        return vel;
    }

    private void ExitJump()
    {
        _state = State.Chase;
    }

    // ── Shield ────────────────────────────────────────────────────────────────

    private void EnterShield()
    {
        _state       = State.Shield;
        _shieldTimer = ShieldDuration;
        _hitBox.EndAttack();
        _sprite.Play(AnimShield);
    }

    private void ExitShield()
    {
        _state = State.Chase;
    }

    // ── Hurt ──────────────────────────────────────────────────────────────────

    private void InterruptIntoHurt()
    {
        if (_state == State.Attack)
        {
            _hitBox.EndAttack();
            _attackDuration = 0;
        }
        _state    = State.Hurt;
        _hurtStun = 0.9f;
        Flash();
        _sprite.Play(AnimHurt);
    }

    private void ExitHurt()
    {
        if (_rng.Randf() < ShieldChanceOnHurt)
            EnterShield();
        else
            _state = State.Chase;
    }

    // ── Animation callback ────────────────────────────────────────────────────

    private void OnAnimationFinished()
    {
        if (_state == State.Dead) return;

        // Only "attack standing" has loop:false — end it here
        // The other two attacks are ended by _attackDuration timer
        if (_sprite.Animation == AnimAttack1 && _state == State.Attack)
            EndAttack();
    }

    // ── Damage received ───────────────────────────────────────────────────────

    private void OnHurt(int damage)
    {
        if (_state == State.Dead) return;

        if (_state == State.Shield)
        {
            int blocked = Mathf.RoundToInt(damage * (1f - ShieldDamageBlock));
            _health -= blocked;
            _sprite.Modulate = new Color(0.4f, 0.6f, 1f);
            CreateTween().TweenProperty(_sprite, "modulate", Colors.White, 0.15f);
        }
        else
        {
            _health -= damage;
            InterruptIntoHurt();
        }

        if (_health <= 0) Die();
    }

    // ── Death ─────────────────────────────────────────────────────────────────

    private void Die()
    {
        _state = State.Dead;
        _hitBox.EndAttack();
        Velocity = Vector2.Zero;
        SetPhysicsProcess(false);
        _sprite.Play(AnimDead);
        GetTree().CreateTimer(1.8).Timeout += QueueFree;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void PlayLoop(string anim)
    {
        if (_sprite.Animation != anim || !_sprite.IsPlaying())
            _sprite.Play(anim);
    }

    private void UpdateHitBoxFacing()
    {
        float x = Mathf.Abs(_hitBoxBaseOffset.X) * (_sprite.FlipH ? -1.0f : 1.0f);
        _hitBox.Position = new Vector2(x, _hitBoxBaseOffset.Y);
    }

    private void Flash()
    {
        _sprite.Modulate = new Color(1f, 0.3f, 0.3f);
        CreateTween().TweenProperty(_sprite, "modulate", Colors.White, 0.2f);
    }

    private Node2D GetPlayer()
    {
        if (_player == null || !IsInstanceValid(_player))
            _player = GetTree().GetFirstNodeInGroup("player") as Node2D;
        return _player;
    }
}
