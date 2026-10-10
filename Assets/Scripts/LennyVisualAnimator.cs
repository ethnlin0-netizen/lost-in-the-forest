using UnityEngine;

/// <summary>Animates Lenny's original artwork without moving the gameplay collider.</summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Movement), typeof(PlayerCombat), typeof(PlayerHealth))]
public sealed class LennyVisualAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer artwork;
    [SerializeField] private Material artworkMaterial;
    [SerializeField] private Rect visiblePixels;
    private Movement movement;
    private PlayerCombat combat;
    private PlayerHealth health;
    private Rigidbody2D body;
    private Transform rig;
    private Mesh mesh;
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock properties;
    private Vector3[] rest, vertices;
    private Vector2[] coordinates;
    private float width, height, clock, gait, lastVelocityY, lastHealth;
    private float attackAt = float.NegativeInfinity, rangedAt = float.NegativeInfinity;
    private float hitAt = float.NegativeInfinity, deathAt = float.NegativeInfinity;
    private float wingUntil, landingUntil;
    private bool chargedAttack, previouslyGrounded;
    public bool IsReady { get; private set; }
    public float AnimationClock => clock;
    public Mesh AnimatedMesh => mesh;
    public int MeleeAnimationsPlayed { get; private set; }
    public int RangedAnimationsPlayed { get; private set; }
    public int HitAnimationsPlayed { get; private set; }
    public int DoubleJumpAnimationsPlayed { get; private set; }

    private void Awake()
    {
        movement = GetComponent<Movement>();
        combat = GetComponent<PlayerCombat>();
        health = GetComponent<PlayerHealth>();
        body = GetComponent<Rigidbody2D>();
        if (artwork == null || artwork.sprite == null || artworkMaterial == null)
        {
            Debug.LogError("Lenny requires the original artwork and animation material.", this);
            enabled = false;
            return;
        }
        BuildMesh();
        properties = new MaterialPropertyBlock();
        combat.MeleePerformed += Melee;
        combat.RangedPerformed += Ranged;
        combat.VisualsReset += ResetAttack;
        health.HealthChanged += Hit;
        lastHealth = health.CurrentHealth;
        previouslyGrounded = movement.IsGrounded;
        lastVelocityY = body.linearVelocity.y;
        artwork.enabled = false;
        IsReady = true;
        Pose();
    }

    private void BuildMesh()
    {
        const int columns = 32, rows = 40;
        var sprite = artwork.sprite;
        width = visiblePixels.width / sprite.pixelsPerUnit * artwork.transform.localScale.x;
        height = visiblePixels.height / sprite.pixelsPerUnit * artwork.transform.localScale.y;
        rig = new GameObject("Lenny Animated Artwork").transform;
        rig.SetParent(artwork.transform.parent, false);
        var shape = GetComponent<CapsuleCollider2D>();
        rig.localPosition = new Vector3(shape.offset.x, shape.offset.y - shape.size.y * .5f, 0);
        meshRenderer = rig.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = artworkMaterial;
        meshRenderer.sortingLayerID = artwork.sortingLayerID;
        meshRenderer.sortingOrder = artwork.sortingOrder;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        rest = new Vector3[(columns + 1) * (rows + 1)];
        vertices = new Vector3[rest.Length];
        coordinates = new Vector2[rest.Length];
        var uv = new Vector2[rest.Length];
        var colors = new Color32[rest.Length];
        for (int y = 0; y <= rows; y++)
        for (int x = 0; x <= columns; x++)
        {
            int i = y * (columns + 1) + x;
            float u = x / (float)columns, v = y / (float)rows;
            coordinates[i] = new Vector2(u, v);
            rest[i] = new Vector3((u - .5f) * width, v * height, 0);
            uv[i] = new Vector2((visiblePixels.x + u * visiblePixels.width) / sprite.texture.width,
                (visiblePixels.y + v * visiblePixels.height) / sprite.texture.height);
            colors[i] = new Color32(255, 255, 255, 255);
        }
        var indices = new int[columns * rows * 6];
        int n = 0;
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < columns; x++)
        {
            int i = y * (columns + 1) + x;
            indices[n++] = i; indices[n++] = i + 1; indices[n++] = i + columns + 2;
            indices[n++] = i; indices[n++] = i + columns + 2; indices[n++] = i + columns + 1;
        }
        mesh = new Mesh { name = "Lenny animated original artwork" };
        mesh.MarkDynamic(); mesh.vertices = rest; mesh.uv = uv; mesh.colors32 = colors; mesh.triangles = indices;
        mesh.bounds = new Bounds(new Vector3(0, height * .5f, 0), new Vector3(width + 2, height + 2, 1));
        rig.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void Melee(bool charged, int direction, float duration, float reach)
    {
        chargedAttack = charged; attackAt = Time.time; MeleeAnimationsPlayed++;
    }
    private void Ranged() { rangedAt = Time.time; RangedAnimationsPlayed++; }
    private void ResetAttack() { attackAt = rangedAt = float.NegativeInfinity; }
    private void Hit()
    {
        if (health.CurrentHealth >= lastHealth) return;
        lastHealth = health.CurrentHealth; hitAt = Time.time; HitAnimationsPlayed++;
        if (health.IsDead) deathAt = Time.time;
    }

    private void LateUpdate()
    {
        if (!IsReady || Time.timeScale <= 0 || Time.deltaTime <= 0) return;
        clock += Time.deltaTime;
        float velocityY = body.linearVelocity.y;
        if (!movement.IsGrounded && !previouslyGrounded && velocityY > 8 && velocityY > lastVelocityY + 3)
        {
            wingUntil = Time.time + .55f; DoubleJumpAnimationsPlayed++;
        }
        if (movement.IsGrounded && !previouslyGrounded) landingUntil = Time.time + .15f;
        previouslyGrounded = movement.IsGrounded; lastVelocityY = velocityY;
        gait += Mathf.Abs(body.linearVelocity.x) * Time.deltaTime * 3.5f;
        Pose();
    }

    private void Pose()
    {
        float walk = movement.IsGrounded ? Mathf.Clamp01(Mathf.Abs(body.linearVelocity.x) / 6f) : 0;
        float jump = movement.IsGrounded ? 0 : Mathf.Clamp(body.linearVelocity.y / 14f, -1, 1);
        float attack = Mathf.Clamp01(1 - (Time.time - attackAt) / .28f);
        float strike = attack > 0 ? Mathf.Sin((1 - attack) * Mathf.PI) : 0;
        float recoil = Mathf.Clamp01(1 - (Time.time - rangedAt) / .22f);
        float hit = Mathf.Clamp01(1 - (Time.time - hitAt) / .2f);
        float landing = Mathf.Clamp01((landingUntil - Time.time) / .15f);
        float charge = 0; // Current combat performs its full-strength tail sweep on press.
        float wing = Mathf.Clamp01((wingUntil - Time.time) / .55f);
        float death = health.IsDead ? Mathf.Clamp01((Time.time - deathAt) / .65f) : 0;
        float breathe = Mathf.Sin(clock * 2.6f) * .007f;
        for (int i = 0; i < vertices.Length; i++)
        {
            var c = coordinates[i]; var p = rest[i];
            float torso = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.12f, .5f, c.y));
            p.y += height * torso * (breathe + Mathf.Abs(Mathf.Sin(gait)) * .025f * walk);
            p.x += height * torso * (.014f * Mathf.Sin(clock * 2) + .035f * strike - .035f * hit - .025f * recoil);
            p.y *= 1 + .035f * jump - .06f * landing - .045f * charge;
            p.x *= 1 - .02f * jump + .035f * landing + .025f * charge;
            float leftFoot = Weight(c, .40f, .08f, .15f, .16f);
            float rightFoot = Weight(c, .74f, .08f, .15f, .16f);
            p.y += height * .035f * walk * (leftFoot * Mathf.Max(0, Mathf.Sin(gait)) + rightFoot * Mathf.Max(0, -Mathf.Sin(gait)));
            p.x += width * .025f * walk * Mathf.Cos(gait) * (leftFoot - rightFoot);
            float leftWing = Weight(c, .20f, .51f, .17f, .14f);
            float rightWing = Weight(c, .81f, .51f, .17f, .14f);
            float flap = Mathf.Sin(clock * (wing > 0 ? 22 : 3)) * (.012f + wing * .10f);
            p.y += height * flap * (leftWing + rightWing);
            p.x += width * wing * .025f * (rightWing - leftWing);
            float tail = Weight(c, .12f, .22f, .23f, .18f);
            p.y += height * tail * (.025f * Mathf.Sin(clock * 3 + gait * .3f) + strike * (chargedAttack ? .12f : .04f));
            p.x += width * tail * strike * (chargedAttack ? .16f : .04f);
            if (death > 0) { p.y *= 1 - .7f * death; p.x += height * c.y * .35f * death; }
            vertices[i] = p;
        }
        mesh.vertices = vertices;
        rig.localScale = new Vector3(movement.FacingDirection, 1, 1);
        properties.SetColor("_Tint", Color.Lerp(artwork.color, new Color(1, .85f, .5f), charge * .25f));
        meshRenderer.SetPropertyBlock(properties);
    }

    private static float Weight(Vector2 p, float x, float y, float radiusX, float radiusY)
    {
        float dx = (p.x - x) / radiusX, dy = (p.y - y) / radiusY;
        return Mathf.Exp(-(dx * dx + dy * dy) * 2);
    }
    private void OnDisable()
    {
        if (artwork != null) artwork.enabled = true;
        if (rig != null) rig.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        if (!IsReady) return;
        artwork.enabled = false; rig.gameObject.SetActive(true);
    }
    private void OnDestroy()
    {
        if (combat != null) { combat.MeleePerformed -= Melee; combat.RangedPerformed -= Ranged; combat.VisualsReset -= ResetAttack; }
        if (health != null) health.HealthChanged -= Hit;
        if (mesh != null) Destroy(mesh);
        if (rig != null) Destroy(rig.gameObject);
    }
}
