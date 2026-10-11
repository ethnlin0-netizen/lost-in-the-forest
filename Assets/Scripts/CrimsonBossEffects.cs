using System.Collections.Generic;
using UnityEngine;

// Scene-local effects; no colliders or changes to the boss physics silhouette.
public sealed class CrimsonBossEffects : MonoBehaviour
{
    public Material effectMaterial;
    private BlightBoss boss;
    private float trailTimer;
    private readonly List<CrimsonBossRibbon> ribbons = new List<CrimsonBossRibbon>();
    private static readonly Color Scarlet = new Color(1f, .23f, .15f);
    private static readonly Color Gold = new Color(1f, .76f, .42f);
    private void OnEnable()
    {
        boss = GetComponent<BlightBoss>();
        boss.AttackPerformed += Burst;
        boss.PhaseChanged += Phase;
    }
    private void Update()
    {
        if (Time.timeScale <= 0f || boss.IsDead) return;
        ribbons.RemoveAll(r => r == null);
        trailTimer -= Time.deltaTime;
        if (boss.State == BlightBoss.BossState.Lunge && trailTimer <= 0f)
        {
            trailTimer = .055f;
            Slash(boss.Feet + Vector2.up * 1.5f, boss.Facing, .8f, .18f);
        }
    }
    public void Slash(Vector2 center, int facing, float reach = 1.6f, float duration = .22f)
    {
        for (int claw = 0; claw < 3; claw++)
        {
            var points = new Vector3[13];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / 12f;
                points[i] = center + new Vector2(facing * (Mathf.Sin(t * Mathf.PI) * reach + claw * .12f), (t - .5f) * 1.6f + claw * .19f);
            }
            Draw(points, claw == 1 ? Gold : Scarlet, .085f, duration);
        }
    }
    private void Burst(BlightBoss.AttackKind attack)
    {
        Vector2 feet = boss.Feet;
        if (attack == BlightBoss.AttackKind.Slam)
        {
            Ring(feet + Vector2.right * boss.Facing * 1.8f, 2.2f, .28f, .24f, Gold);
            Ring(feet + Vector2.right * boss.Facing * 1.8f, 1.3f, .48f, .12f, Scarlet);
        }
        else if (attack == BlightBoss.AttackKind.Lunge) Slash(feet + Vector2.up * 1.6f, boss.Facing);
        else if (attack == BlightBoss.AttackKind.Spores) Ring(feet + new Vector2(boss.Facing * 1.7f, 1.15f), .6f, .6f, .1f, new Color(1f,.42f,.8f));
        else Ring(feet, 1.5f, .24f, .13f, Scarlet);
    }
    private void Phase() { Ring(boss.Feet + Vector2.up * 1.8f, 3f, 1f, .12f, Scarlet); }
    private void Ring(Vector2 center, float radius, float flatten, float width, Color color)
    {
        var points = new Vector3[41];
        for(int i=0;i<points.Length;i++){float a=i*Mathf.PI*2/40;points[i]=center+new Vector2(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius*flatten);}
        Draw(points,color,width,.38f);
    }
    private void Draw(Vector3[] points, Color color, float width, float lifetime)
    {
        if(effectMaterial == null) return;
        var go = new GameObject("Crimson attack ribbon");
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial=effectMaterial;line.useWorldSpace=true;line.positionCount=points.Length;line.SetPositions(points);
        line.sortingOrder=12;line.numCapVertices=2;line.numCornerVertices=2;
        line.widthCurve=new AnimationCurve(new Keyframe(0,.15f),new Keyframe(.35f,1),new Keyframe(1,0));line.widthMultiplier=width;
        var fade=go.AddComponent<CrimsonBossRibbon>();fade.Initialize(boss,line,color,lifetime);ribbons.Add(fade);
    }
    private void OnDisable()
    {
        if(boss!=null){boss.AttackPerformed-=Burst;boss.PhaseChanged-=Phase;}
        foreach(var ribbon in ribbons)if(ribbon!=null)Destroy(ribbon.gameObject);ribbons.Clear();
    }
}

public sealed class CrimsonBossRibbon : MonoBehaviour
{
    private BlightBoss owner;
    private LineRenderer line;
    private Color color;
    private float elapsed,duration;
    public void Initialize(BlightBoss source,LineRenderer renderer,Color tint,float lifetime){owner=source;line=renderer;color=tint;duration=lifetime;line.startColor=line.endColor=tint;}
    private void Update()
    {
        if(owner==null||owner.IsDead||!owner.isActiveAndEnabled){Destroy(gameObject);return;}
        elapsed+=Time.deltaTime;
        if(elapsed>=duration){Destroy(gameObject);return;}
        var tint=color;tint.a*=1-elapsed/duration;line.startColor=line.endColor=tint;
    }
}
