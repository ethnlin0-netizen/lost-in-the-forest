using UnityEngine;

// State-driven poses on an isolated artwork pivot; never moves the physics hull.
[DefaultExecutionOrder(100)]
public sealed class FusedBossAnimator : MonoBehaviour
{
    public Transform artwork;
    private BlightBoss boss;
    private EnemyHealth health;
    private Rigidbody2D body;
    private Vector3 restPosition, restScale;
    private Quaternion restRotation;
    private float motionTime, impactTime = -100f, deathTime;
    private BlightBoss.AttackKind impactAttack;
    private bool dead;
    private void Awake()
    {
        boss=GetComponent<BlightBoss>();health=GetComponent<EnemyHealth>();body=GetComponent<Rigidbody2D>();
        if(artwork==null){enabled=false;return;}
        restPosition=artwork.localPosition;restScale=artwork.localScale;restRotation=artwork.localRotation;
    }
    private void OnEnable() { boss=GetComponent<BlightBoss>();health=GetComponent<EnemyHealth>();boss.AttackPerformed+=Impact;health.Died+=Death; }
    private void Impact(BlightBoss.AttackKind attack) { impactAttack=attack;impactTime=motionTime; }
    private void Death() { dead=true;deathTime=motionTime; }
    private void LateUpdate()
    {
        if(artwork==null || Time.timeScale<=0f)return;
        motionTime+=Time.deltaTime;
        float x=1f,y=1f,angle=0f,lift=0f,lean=0f;
        if(dead){float t=Mathf.Clamp01((motionTime-deathTime)/1.15f);x=1f-.18f*t;y=1f-.85f*t;angle=-12f*t;}
        else{
            float stride=Mathf.Sin(motionTime*(boss.PhaseTwo?8f:6f));
            bool walking=Mathf.Abs(body.linearVelocity.x)>.1f;
            y=1f+Mathf.Sin(motionTime*2.4f)*.012f;
            if(walking){lift=Mathf.Abs(stride)*.075f;angle=stride*1.6f;}
            float p=boss.WindupProgress;
            if(boss.State==BlightBoss.BossState.Windup){
                switch(boss.CurrentAttack){
                    case BlightBoss.AttackKind.Slam:x=1f-.075f*p;y=1f+.085f*p;angle=7f*p;break;
                    case BlightBoss.AttackKind.Lunge:x=1f+.12f*p;y=1f-.08f*p;lean=-.14f*p;angle=8f*p;break;
                    case BlightBoss.AttackKind.Spores:x=1f+.045f*p;y=1f+.055f*p;lift=.055f*p;break;
                    case BlightBoss.AttackKind.Thorns:x=1f+.08f*p;y=1f-.12f*p;angle=Mathf.Sin(motionTime*25f)*p*1.4f;break;
                }
            }
            if(boss.State==BlightBoss.BossState.Lunge){x=1.14f;y=.9f;angle=-9f;lean=.1f;}
            float impact=Mathf.Clamp01(1f-(motionTime-impactTime)/.32f);
            if(impactAttack==BlightBoss.AttackKind.Slam||impactAttack==BlightBoss.AttackKind.Thorns){x+=impact*.13f;y-=impact*.14f;angle-=impact*8f;}
            else if(impactAttack==BlightBoss.AttackKind.Spores){x-=impact*.06f;lean-=impact*.1f;}
            if(boss.State==BlightBoss.BossState.PhaseTransition){x+=Mathf.Sin(motionTime*22f)*.025f;y+=.05f;angle=Mathf.Sin(motionTime*25f)*2f;}
            if(Time.time-health.LastHitTime<.15f){lean-=.09f;angle+=3f;}
        }
        artwork.localScale=Vector3.Scale(restScale,new Vector3(x,y,1f));
        // Compensate scale around the feet rather than stretching into the floor.
        var sprite=artwork.GetComponent<SpriteRenderer>().sprite;
        float halfHeight=sprite.bounds.extents.y*restScale.y;
        artwork.localPosition=restPosition+new Vector3(lean,halfHeight*(y-1f)+lift,0f);
        artwork.localRotation=restRotation*Quaternion.Euler(0,0,angle);
    }
    private void OnDisable(){if(boss!=null)boss.AttackPerformed-=Impact;if(health!=null)health.Died-=Death;}
}
