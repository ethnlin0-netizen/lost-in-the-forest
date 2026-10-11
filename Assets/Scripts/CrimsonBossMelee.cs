using UnityEngine;

// A short, committed swipe supplements the large ability cycle at close range.
public sealed class CrimsonBossMelee : MonoBehaviour
{
    public float damage = 14f;
    public float reach = 1.15f;
    public float windup = .22f;
    public float cooldown = .85f;
    private BlightBoss boss;
    private Movement player;
    private BoxCollider2D hull;
    private float remaining, warning;
    private int facing;
    private bool pending;
    private void Awake(){boss=GetComponent<BlightBoss>();hull=GetComponent<BoxCollider2D>();player=FindAnyObjectByType<Movement>();}
    private bool InReach()
    {
        if(player==null||!player.isActiveAndEnabled)return false;
        var health=player.GetComponent<PlayerHealth>();var target=player.GetComponent<Collider2D>();
        if(health==null||health.IsDead||target==null)return false;
        return Mathf.Abs(target.bounds.center.x-hull.bounds.center.x)<=hull.bounds.extents.x+target.bounds.extents.x+reach &&
            Mathf.Abs(target.bounds.min.y-hull.bounds.min.y)<1.2f && boss.HasLineOfSight(hull.bounds.center,target.bounds.center);
    }
    private void Update()
    {
        if(Time.timeScale<=0f)return;
        if(boss.IsDead||!boss.isActiveAndEnabled||boss.State==BlightBoss.BossState.PhaseTransition){pending=false;return;}
        remaining=Mathf.Max(0,remaining-Time.deltaTime);
        if(pending)
        {
            warning-=Time.deltaTime;
            if(warning>0)return;
            pending=false;
            GetComponent<CrimsonBossEffects>()?.Slash(boss.Feet+Vector2.up*1.1f,facing,1.45f);
            if(InReach()&&Mathf.Sign(player.transform.position.x-hull.bounds.center.x)==facing)
                player.GetComponent<PlayerHealth>().TakeDamage(damage,Vector2.right*facing,1.2f);
            GetComponent<GameplaySfx>()?.Play(GameSound.BossLunge);
        }
        else if(remaining<=0&&InReach())
        {
            facing=player.transform.position.x>=hull.bounds.center.x?1:-1;pending=true;warning=windup;remaining=cooldown;
            GetComponent<CrimsonBossEffects>()?.Slash(boss.Feet+Vector2.up*1.1f,facing,.45f,windup);
        }
    }
    private void OnDisable(){pending=false;}
}
