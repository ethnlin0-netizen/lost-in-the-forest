using UnityEngine;

[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
public sealed class GameplaySfx : MonoBehaviour
{
    public enum Role { Player, Chomper, Spora, Collusus, Boss, Whisperseed, Dryad, Petalborn, Fireball, Spore, Shockwave, Crystal }
    public Role role;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0.1f)] public float stepDistance = 1.7f;
    private Movement movement;
    private PlayerCombat combat;
    private PlayerHealth playerHealth;
    private EnemyHealth enemyHealth;
    private BlightEnemy enemy;
    private BlightBoss boss;
    private Rigidbody2D body;
    private Collider2D shape;
    private EnemyNavigator2D navigator;
    private float lastHealth, distance;
    private Vector2 previousPosition;
    private int warning;
    private GameAudioController warningController;
    private readonly RaycastHit2D[] ground = new RaycastHit2D[8];
    public int Play(GameSound sound) => GameAudioController.Instance == null ? 0 : GameAudioController.Instance.Play(sound, this, volume);
    public void Impact() => Play(role == Role.Fireball ? GameSound.FireImpact : role == Role.Shockwave ? GameSound.BossShockwave : GameSound.SporeImpact);
    public void Recruit() => Play(role == Role.Whisperseed ? GameSound.WhisperseedRecruit : role == Role.Dryad ? GameSound.DryadRecruit : GameSound.PetalbornRecruit);
    public void BeginWarning(GameSound sound)
    {
        StopWarning(); warningController = GameAudioController.Instance;
        if (warningController != null) warning = warningController.Play(sound, this, volume);
    }
    public void StopWarning() { if (warningController != null) warningController.Cancel(warning); warning = 0; warningController = null; }

    private void OnEnable()
    {
        body = GetComponent<Rigidbody2D>(); shape = GetComponent<Collider2D>(); navigator = GetComponent<EnemyNavigator2D>();
        previousPosition = transform.position; distance = 0;
        movement = GetComponent<Movement>(); combat = GetComponent<PlayerCombat>();
        playerHealth = GetComponent<PlayerHealth>(); enemyHealth = GetComponent<EnemyHealth>();
        enemy = GetComponent<BlightEnemy>(); boss = GetComponent<BlightBoss>();
        if (movement != null) { movement.Jumped += Jump; movement.DoubleJumped += DoubleJump; movement.Landed += Land; }
        if (combat != null) { combat.MeleePerformed += Melee; combat.RangedPerformed += Fire; }
        if (playerHealth != null) { lastHealth = playerHealth.CurrentHealth; playerHealth.HealthChanged += HealthChanged; }
        if (enemyHealth != null) { lastHealth = enemyHealth.CurrentHealth; enemyHealth.HealthChanged += HealthChanged; }
        if (enemy != null) { enemy.WindupStarted += EnemyWarning; enemy.AttackExecuted += EnemyAttack; enemy.AttackCanceled += StopWarning; }
        if (boss != null) { boss.AttackWindup += BossWarning; boss.AttackPerformed += BossAttack; boss.AttackCanceled += StopWarning; boss.PhaseChanged += Phase; }
    }

    private void Jump() => Play(GameSound.Jump);
    private void DoubleJump() => Play(GameSound.DoubleJump);
    private void Land(float speed) { if (speed > 1.5f) Play(speed > 12f ? GameSound.HardLand : GameSound.Land); }
    private void Melee(bool charged, int direction, float duration, float reach) => Play(GameSound.TailSweep);
    private void Fire() => Play(GameSound.FireLaunch);
    private void HealthChanged()
    {
        float health = playerHealth != null ? playerHealth.CurrentHealth : enemyHealth.CurrentHealth;
        if (health >= lastHealth) { lastHealth = health; return; }
        lastHealth = health;
        if (health <= 0 || enemy != null) StopWarning();
        GameSound sound = role == Role.Player ? (health <= 0 ? GameSound.PlayerDeath : GameSound.PlayerHurt) :
            role == Role.Chomper ? (health <= 0 ? GameSound.ChomperDeath : GameSound.ChomperHit) :
            role == Role.Spora ? (health <= 0 ? GameSound.SporaDeath : GameSound.SporaHit) :
            role == Role.Collusus ? (health <= 0 ? GameSound.RockDeath : GameSound.RockHit) :
            (health <= 0 ? GameSound.BossDeath : GameSound.BossHit);
        Play(sound);
    }
    private void EnemyWarning() => BeginWarning(role == Role.Chomper ? GameSound.ChomperWarn : role == Role.Spora ? GameSound.SporaWarn : GameSound.RockWarn);
    private void EnemyAttack() { StopWarning(); Play(role == Role.Chomper ? GameSound.ChomperBite : role == Role.Spora ? GameSound.SporaLaunch : GameSound.RockSlam); }
    private void BossWarning(BlightBoss.AttackKind attack) => BeginWarning(attack == BlightBoss.AttackKind.Slam ? GameSound.BossSlamWarn : attack == BlightBoss.AttackKind.Lunge ? GameSound.BossLungeWarn : attack == BlightBoss.AttackKind.Thorns ? GameSound.BossThornWarn : GameSound.BossSporeWarn);
    private void BossAttack(BlightBoss.AttackKind attack) { StopWarning(); Play(attack == BlightBoss.AttackKind.Slam ? GameSound.BossSlam : attack == BlightBoss.AttackKind.Lunge ? GameSound.BossLunge : attack == BlightBoss.AttackKind.Thorns ? GameSound.BossThorns : GameSound.BossVolley); if (attack == BlightBoss.AttackKind.Slam) Play(GameSound.BossShockwave); }
    private void Phase() { StopWarning(); Play(GameSound.BossPhase); }

    private void FixedUpdate()
    {
        Vector2 position = transform.position; float travel = Mathf.Abs(position.x - previousPosition.x); previousPosition = position;
        if (role != Role.Player && role != Role.Collusus && role != Role.Boss) return;
        if (Time.timeScale <= 0 || AudioListener.pause || body == null || !body.simulated || Mathf.Abs(body.linearVelocity.x) < 0.2f || travel > 1f ||
            playerHealth != null && playerHealth.IsDead || enemyHealth != null && enemyHealth.IsDead) { distance = 0; return; }
        bool grounded = movement != null ? movement.enabled && movement.IsGrounded : navigator != null ? navigator.IsGrounded : OnGround();
        if (!grounded) { distance = 0; return; }
        distance += travel;
        if (distance >= stepDistance) { distance %= stepDistance; Play(role == Role.Player ? GameSound.Footstep : GameSound.RockStep); }
    }
    private bool OnGround()
    {
        if (shape == null || body.linearVelocity.y > 0.1f) return false;
        int count = shape.Cast(Vector2.down, new ContactFilter2D { useTriggers = false }, ground, 0.08f);
        for (int i = 0; i < count; i++) if (ground[i].normal.y > 0.5f) return true;
        return false;
    }
    private void OnDisable()
    {
        StopWarning();
        if (movement != null) { movement.Jumped -= Jump; movement.DoubleJumped -= DoubleJump; movement.Landed -= Land; }
        if (combat != null) { combat.MeleePerformed -= Melee; combat.RangedPerformed -= Fire; }
        if (playerHealth != null) playerHealth.HealthChanged -= HealthChanged;
        if (enemyHealth != null) enemyHealth.HealthChanged -= HealthChanged;
        if (enemy != null) { enemy.WindupStarted -= EnemyWarning; enemy.AttackExecuted -= EnemyAttack; enemy.AttackCanceled -= StopWarning; }
        if (boss != null) { boss.AttackWindup -= BossWarning; boss.AttackPerformed -= BossAttack; boss.AttackCanceled -= StopWarning; boss.PhaseChanged -= Phase; }
    }
}
