using UnityEngine;

/// <summary>Animates the original artwork with a UV mesh; gameplay bodies never deform.</summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(BlightEnemy), typeof(EnemyHealth))]
public sealed class EnemyVisualAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer artwork;
    [SerializeField] private Material artworkMaterial;
    [SerializeField] private Material particleMaterial;
    [Tooltip("Visible artwork bounds in source texture pixels, excluding transparent padding.")]
    [SerializeField] private Rect visiblePixels;
    [SerializeField, Range(16, 48)] private int meshColumns = 32;
    [SerializeField, Range(20, 56)] private int meshRows = 40;
    [SerializeField, Min(.5f)] private float deathDuration = 1.8f;

    private BlightEnemy enemy;
    private EnemyHealth health;
    private Rigidbody2D body;
    private EnemyNavigator2D navigation;
    private Transform rig;
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock properties;
    private ParticleSystem particles;
    private Vector3[] resting, deformed;
    private Vector2[] coordinates;
    private int[] pieces;
    private float width, height, clock, gait, windupBlend, strikeAt = float.NegativeInfinity;
    private float deathAt = float.NegativeInfinity, nextStepParticle, previousFootCycle;
    private float observedHit = float.NegativeInfinity;
    private int observedAttacks;
    private bool previousGrounded, initialized;

    public float DeathDuration => deathDuration;
    public bool IsReady => initialized;
    public float AnimationClock => clock;
    public int AttackAnimationsPlayed { get; private set; }
    public int HitAnimationsPlayed { get; private set; }
    public int ParticlesEmitted { get; private set; }
    public Mesh AnimatedMesh => mesh;

    private void Awake()
    {
        enemy = GetComponent<BlightEnemy>();
        health = GetComponent<EnemyHealth>();
        body = GetComponent<Rigidbody2D>();
        navigation = GetComponent<EnemyNavigator2D>();
        if (artwork == null || artwork.sprite == null || artworkMaterial == null || particleMaterial == null)
        {
            Debug.LogError("Enemy animation requires artwork and both materials.", this);
            enabled = false;
            return;
        }
        BuildRig();
        BuildParticles();
        properties = new MaterialPropertyBlock();
        health.Died += BeginDeath;
        observedAttacks = enemy.AttacksPerformed;
        previousGrounded = navigation.IsGrounded;
        clock = Mathf.Abs(transform.position.x)*.37f;
        initialized = true;
        artwork.enabled = false;
        Pose(0f);
    }

    private void BuildRig()
    {
        var sprite = artwork.sprite;
        var crop = visiblePixels;
        width = crop.width/sprite.pixelsPerUnit*artwork.transform.localScale.x;
        height = crop.height/sprite.pixelsPerUnit*artwork.transform.localScale.y;
        rig = new GameObject("AnimatedArtworkRig").transform;
        rig.SetParent(artwork.transform.parent, false);
        var shape = GetComponent<BoxCollider2D>();
        rig.localPosition = new Vector3(shape.offset.x, shape.offset.y-shape.size.y*.5f, 0f);
        rig.gameObject.layer = artwork.gameObject.layer;
        meshRenderer = rig.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = artworkMaterial;
        meshRenderer.sortingLayerID = artwork.sortingLayerID;
        meshRenderer.sortingOrder = artwork.sortingOrder;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        var filter = rig.gameObject.AddComponent<MeshFilter>();
        int count = meshColumns*meshRows*6;
        resting = new Vector3[count];
        deformed = new Vector3[count];
        coordinates = new Vector2[count];
        pieces = new int[count];
        var uv = new Vector2[count];
        var indices = new int[count];
        var colors = new Color32[count];
        int index = 0;
        for (int row=0; row<meshRows; row++)
        for (int column=0; column<meshColumns; column++)
        {
            int piece = StonePiece((column+.5f)/meshColumns, (row+.5f)/meshRows);
            for (int corner=0; corner<6; corner++)
            {
                int dx = corner==1 || corner==2 || corner==4 ? 1 : 0;
                int dy = corner==2 || corner==4 || corner==5 ? 1 : 0;
                float x = (column+dx)/(float)meshColumns, y = (row+dy)/(float)meshRows;
                resting[index] = new Vector3((x-.5f)*width, y*height, 0f);
                coordinates[index] = new Vector2(x-.5f, y);
                pieces[index] = piece;
                uv[index] = new Vector2((crop.x+x*crop.width)/sprite.texture.width,
                    (crop.y+y*crop.height)/sprite.texture.height);
                indices[index] = index;
                colors[index] = new Color32(255,255,255,255);
                index++;
            }
        }
        mesh = new Mesh { name = enemy.Kind+" animated artwork" };
        mesh.MarkDynamic();
        mesh.vertices = resting;
        mesh.uv = uv;
        mesh.colors32 = colors;
        mesh.triangles = indices;
        // Includes the breakup trajectories; avoids culling a dying limb prematurely.
        mesh.bounds = new Bounds(new Vector3(0,height*.5f,0),new Vector3(width+4,height+4,1));
        filter.sharedMesh = mesh;
    }

    private static int StonePiece(float x, float y)
    {
        if (y < .27f) return x < .5f ? 4 : 5;
        if (x < .23f && y < .67f) return 2;
        if (x > .82f && y < .67f) return 3;
        if (y > .68f) return 0;
        return x < .5f ? 1 : 6;
    }

    private void BuildParticles()
    {
        var effect = new GameObject(enemy.Kind+" petals spores or stone dust");
        effect.transform.SetParent(transform, false);
        particles = effect.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = false;
        main.maxParticles = 100;
        main.startLifetime = 1f;
        var emission = particles.emission; emission.enabled = false;
        var shape = particles.shape; shape.enabled = false;
        var color = particles.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
            new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.08f),new GradientAlphaKey(.6f,.65f),new GradientAlphaKey(0,1)});
        color.color = gradient;
        var rotation = particles.rotationOverLifetime;
        rotation.enabled = enemy.Kind != BlightEnemy.BlightType.Spora;
        rotation.z = new ParticleSystem.MinMaxCurve(-3f,3f);
        var renderer = effect.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.sortingLayerID = artwork.sortingLayerID;
        renderer.sortingOrder = artwork.sortingOrder+1;
        particles.Play();
    }

    private void BeginDeath()
    {
        deathAt = Time.time;
        var center = transform.TransformPoint(new Vector3(0,height*.3f,0));
        Emit(enemy.Kind==BlightEnemy.BlightType.Spora ? 24 : 18, center, true);
    }

    private void LateUpdate()
    {
        if (!initialized || Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
        float dt = Time.deltaTime;
        clock += dt;
        float speed = Mathf.Abs(body.linearVelocity.x);
        gait += dt*speed*(enemy.Kind==BlightEnemy.BlightType.Collusus ? 3.8f : 7f);
        float targetWindup = enemy.State==BlightEnemy.BehaviourState.Windup ? enemy.WindupProgress : 0f;
        windupBlend = Mathf.MoveTowards(windupBlend,targetWindup,dt*5f);
        if (enemy.AttacksPerformed != observedAttacks)
        {
            observedAttacks = enemy.AttacksPerformed;
            strikeAt = Time.time;
            AttackAnimationsPlayed++;
            if (enemy.Kind==BlightEnemy.BlightType.Spora)
                Emit(9,transform.TransformPoint(new Vector3(enemy.Facing*width*.25f,height*.1f,0)),false);
            else if (enemy.Kind==BlightEnemy.BlightType.Collusus)
                Emit(8,transform.TransformPoint(new Vector3(enemy.Facing*width*.5f,-height*.5f,0)),false);
        }
        if (health.LastHitTime > observedHit)
        {
            observedHit = health.LastHitTime;
            HitAnimationsPlayed++;
            if (enemy.Kind==BlightEnemy.BlightType.Collusus && !health.IsDead)
                Emit(6,transform.position,false);
        }
        bool grounded = navigation.IsGrounded;
        float footCycle = Mathf.Sin(gait);
        if (!health.IsDead && enemy.Kind==BlightEnemy.BlightType.Collusus && grounded)
        {
            bool step = speed>.1f && footCycle*previousFootCycle<0f && Time.time>=nextStepParticle;
            if (step || !previousGrounded)
            {
                Emit(4,transform.position+Vector3.down*height*.5f,false);
                nextStepParticle = Time.time+.18f;
            }
        }
        previousFootCycle = footCycle;
        previousGrounded = grounded;
        Pose(speed);
    }

    private void Pose(float speed)
    {
        float strike = StrikeEnvelope();
        float hitAge = Time.time-health.LastHitTime;
        float hit = hitAge>=0 && hitAge<.35f ? Mathf.Exp(-hitAge*10f)*Mathf.Cos(hitAge*22f) : 0f;
        float hitDirection = Mathf.Abs(health.LastHitDirection.x)>.01f
            ? Mathf.Sign(health.LastHitDirection.x)*enemy.Facing : -1f;
        float death = health.IsDead ? Mathf.Clamp01((Time.time-deathAt)/deathDuration) : 0f;
        float moving = Mathf.Clamp01(speed/(enemy.Kind==BlightEnemy.BlightType.Collusus ? 1.5f : 2.5f));
        for (int i=0; i<resting.Length; i++)
        {
            Vector2 uv = coordinates[i];
            Vector2 p;
            if (enemy.Kind==BlightEnemy.BlightType.Chomper)
                p = PlantPose(uv,moving,strike,hit*hitDirection,death);
            else if (enemy.Kind==BlightEnemy.BlightType.Spora)
                p = MushroomPose(uv,strike,hit*hitDirection,death);
            else
                p = StonePose(uv,pieces[i],moving,strike,hit*hitDirection,death);
            deformed[i] = new Vector3(p.x*width,p.y*height,0f);
        }
        mesh.vertices = deformed;
        float alpha = health.IsDead ? 1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1f,death)) : 1f;
        var tint = Color.Lerp(Color.white,new Color(1f,.35f,.45f),Mathf.Clamp01(hitAge>=0 && hitAge<.16f ? 1-hitAge/.16f : 0));
        tint.a = alpha;
        properties.SetColor("_Tint",tint);
        float blink = 0f;
        if (enemy.Kind==BlightEnemy.BlightType.Collusus && !health.IsDead)
        {
            float cycle = Mathf.Repeat(clock,4.7f);
            blink = cycle<.16f ? Mathf.Sin(cycle/.16f*Mathf.PI) : 0f;
        }
        properties.SetFloat("_Blink",blink);
        meshRenderer.SetPropertyBlock(properties);
    }

    private float StrikeEnvelope()
    {
        float age = Time.time-strikeAt;
        // Peak is at the actual damage/projectile frame, followed by recovery.
        return age>=0 && age<.32f ? 1f-Mathf.SmoothStep(0,1,age/.32f) : 0f;
    }

    private Vector2 PlantPose(Vector2 p,float moving,float strike,float hit,float death)
    {
        float y = p.y;
        float crown = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,.95f,y));
        float sway = Mathf.Sin(clock*1.7f+y*2f)*.035f;
        p.x += y*y*(sway-Mathf.Sin(gait)*.065f*moving-.18f*windupBlend+.27f*strike+.18f*hit);
        float twitch = Mathf.Pow(Mathf.Max(0,Mathf.Sin(clock*2.1f)),14f);
        p.x *= 1f+crown*(twitch*.08f+strike*.28f);
        p.y += crown*.018f*Mathf.Sin(clock*5f+p.x*8f);
        float rootWeight = 1f-Mathf.SmoothStep(0,.24f,y);
        p.x += rootWeight*Mathf.Sin(gait+p.x*8)*.055f*moving;
        p.y += rootWeight*Mathf.Max(0,Mathf.Sin(gait+p.x*9))*.035f*moving;
        p.y *= 1f-.1f*windupBlend+.055f*strike;
        if (health.IsDead)
        {
            float curl = Mathf.SmoothStep(0,1,death);
            p.x += y*y*curl*.42f;
            p.y *= 1f-.78f*curl;
            p.x *= 1f-.25f*curl;
            p.y -= crown*curl*.12f;
        }
        return p;
    }

    private Vector2 MushroomPose(Vector2 p,float strike,float hit,float death)
    {
        float cap = Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.68f,p.y));
        float breath = Mathf.Sin(clock*2.6f);
        float bounce = strike>0f ? Mathf.Sin((Time.time-strikeAt)*28f)*strike : 0f;
        float vertical = 1f+.025f*breath-.19f*windupBlend+.18f*strike-.12f*Mathf.Abs(hit);
        p.x *= 1f-.018f*breath+.16f*windupBlend-.1f*strike+.09f*Mathf.Abs(hit);
        p.x += p.y*hit*.12f;
        p.y *= vertical;
        p.y += cap*(.018f*Mathf.Sin(clock*3f-p.x*6f)+.035f*bounce);
        p.x += cap*.014f*Mathf.Sin(clock*3f-1f+p.y*7f);
        if (health.IsDead)
        {
            float slump = Mathf.SmoothStep(0,1,death);
            p.x *= 1f+.25f*Mathf.Sin(slump*Mathf.PI);
            p.y *= 1f-.87f*slump;
            p.y -= cap*Mathf.Abs(p.x)*slump*.12f;
        }
        return p;
    }

    private Vector2 StonePose(Vector2 p,int piece,float moving,float strike,float hit,float death)
    {
        Vector2 pivot = StonePivot(piece);
        float angle = 0f;
        Vector2 offset = Vector2.zero;
        if (piece==2 || piece==3)
        {
            float side = piece==2 ? -1f : 1f;
            // Fade joint motion to zero at every shared cell edge. This keeps
            // the original outline connected until the deliberate death breakup.
            float attachment = piece==2 ? Mathf.InverseLerp(-.30f,-.46f,p.x) : Mathf.InverseLerp(.32f,.46f,p.x);
            float weight = Mathf.SmoothStep(0,1,attachment)
                *Mathf.SmoothStep(0,1,Mathf.InverseLerp(.30f,.37f,p.y))
                *(1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.58f,.65f,p.y)));
            angle = side*(Mathf.Sin(gait)*10f*moving-32f*windupBlend+65f*strike)*weight;
            offset.y = (windupBlend*.045f-strike*.1f)*weight;
        }
        else if (piece==4 || piece==5)
        {
            float step = Mathf.Sin(gait+(piece==4 ? 0f : Mathf.PI));
            float weight = (1f-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.25f,p.y)))
                *Mathf.SmoothStep(0,1,Mathf.InverseLerp(.06f,.16f,Mathf.Abs(p.x)));
            angle = step*12f*moving*weight;
            offset.x = step*.035f*moving*weight;
            offset.y = Mathf.Max(0,step)*.08f*moving*weight;
            if (!navigation.IsGrounded) offset.y += .035f*weight;
        }
        float lean = Mathf.Sin(clock*.8f)*.5f+Mathf.Sin(gait)*2.5f*moving-4f*windupBlend+7f*strike+hit*6f;
        p = Rotate(p,pivot,angle)+offset;
        p = Rotate(p,new Vector2(0,.3f),lean);
        p.x += strike*.055f+hit*.045f;
        if (health.IsDead)
        {
            float age = Mathf.Max(0,Time.time-deathAt);
            float separation = Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.22f));
            float side = piece==2 || piece==4 || piece==1 ? -1f : 1f;
            float spread = piece==0 ? 0f : piece==1 || piece==6 ? .12f : .32f;
            p = Rotate(p,pivot,side*age*(piece==0 ? 12f : 65f));
            p.x += side*spread*separation+side*age*.08f;
            float launch = piece==0 ? .55f : piece==2 || piece==3 ? .22f : .1f;
            float fall = launch*age-.7f*age*age;
            float floor = -pivot.y+.055f;
            p.y += Mathf.Max(fall,floor);
        }
        return p;
    }

    private static Vector2 StonePivot(int piece)
    {
        switch(piece)
        {
            case 2: return new Vector2(-.33f,.46f);
            case 3: return new Vector2(.34f,.46f);
            case 4: return new Vector2(-.22f,.13f);
            case 5: return new Vector2(.24f,.13f);
            case 0: return new Vector2(0,.76f);
            default: return new Vector2(piece==1 ? -.12f : .12f,.45f);
        }
    }

    private static Vector2 Rotate(Vector2 p,Vector2 pivot,float degrees)
    {
        float angle=degrees*Mathf.Deg2Rad, sin=Mathf.Sin(angle), cos=Mathf.Cos(angle);
        p-=pivot;
        return pivot+new Vector2(p.x*cos-p.y*sin,p.x*sin+p.y*cos);
    }

    private void Emit(int count,Vector3 position,bool dying)
    {
        if (particles==null || Time.timeScale<=0f) return;
        bool mushroom = enemy.Kind==BlightEnemy.BlightType.Spora;
        bool stone = enemy.Kind==BlightEnemy.BlightType.Collusus;
        var main=particles.main;
        main.gravityModifier = mushroom ? -.03f : stone ? .35f : .08f;
        if (!particles.isPlaying) particles.Play();
        for(int i=0;i<count;i++)
        {
            float angle=Random.Range(0f,Mathf.PI*2f);
            var emit = new ParticleSystem.EmitParams
            {
                position=position+new Vector3(Random.Range(-width*.18f,width*.18f),Random.Range(-.08f,.12f),0),
                velocity=new Vector3(Mathf.Cos(angle)*(stone ? .8f : .35f),mushroom ? Random.Range(.2f,.6f) : Random.Range(.2f,.9f),0),
                startLifetime=dying ? Mathf.Min(deathDuration-.15f,1.35f) : .65f,
                startSize=mushroom ? Random.Range(.09f,.2f) : Random.Range(.045f,.095f),
                startColor=mushroom ? new Color(.65f,.3f,.88f,.65f) : stone ? new Color(.64f,.55f,.8f) : new Color(1f,.3f,.53f),
                rotation=Random.Range(0f,360f)
            };
            particles.Emit(emit,1);
        }
        ParticlesEmitted += count;
    }

    private void OnDisable()
    {
        if (!initialized) return;
        if (meshRenderer!=null) meshRenderer.enabled=false;
        if (artwork!=null) artwork.enabled=true;
        if (particles!=null) particles.Pause();
    }

    private void OnEnable()
    {
        if (!initialized) return;
        meshRenderer.enabled=true;
        artwork.enabled=false;
        if (particles!=null) particles.Play();
    }

    private void OnDestroy()
    {
        if (health!=null) health.Died-=BeginDeath;
        if (mesh!=null) Destroy(mesh);
        if (rig!=null) Destroy(rig.gameObject);
        if (particles!=null) Destroy(particles.gameObject);
    }
}
