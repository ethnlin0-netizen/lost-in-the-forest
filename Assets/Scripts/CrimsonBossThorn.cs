using UnityEngine;

// Ground markings give a full dodge window before a short, bounded damage burst.
public sealed class CrimsonBossThorn : MonoBehaviour
{
    private BlightBoss owner;
    private float elapsed, warningTime, damage;
    private bool struck;
    private Mesh mesh;
    private Material material;
    private MeshRenderer artwork;
    private readonly Collider2D[] overlaps = new Collider2D[12];
    public bool IsActive => elapsed >= warningTime && elapsed < warningTime + 0.45f;

    public void Initialize(BlightBoss source, float warning, float amount)
    {
        owner = source; warningTime = warning; damage = amount;
        mesh = new Mesh { name = "Crimson thorn silhouette" };
        mesh.vertices = new[] { new Vector3(-.52f,0,0), new Vector3(-.31f,.63f,0), new Vector3(.02f,1.5f,0), new Vector3(-.06f,.5f,0), new Vector3(.08f,0,0),
            new Vector3(.05f,0,0), new Vector3(.26f,.5f,0), new Vector3(.5f,1.05f,0),new Vector3(.44f,.35f,0), new Vector3(.55f,0,0) };
        mesh.triangles = new[] { 0,1,3,1,2,3,0,3,4,5,6,8,6,7,8,5,8,9 };
        mesh.colors = new[]{Color.black,new Color(.65f,.04f,.1f),new Color(1,.65f,.4f),new Color(.65f,.04f,.1f),Color.black,Color.black,new Color(.65f,.04f,.1f),new Color(1,.65f,.4f),new Color(.65f,.04f,.1f),Color.black};
        mesh.uv = new Vector2[10]; mesh.RecalculateBounds();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        artwork = gameObject.AddComponent<MeshRenderer>();
        material = new Material(Shader.Find("Sprites/Default"));
        material.color = new Color(1f,.16f,.3f,.85f);
        artwork.sharedMaterial = material; artwork.sortingOrder = 8;
        transform.localScale = new Vector3(1f,.045f,1f);
    }
    private void Update()
    {
        if (owner == null || owner.IsDead || !owner.isActiveAndEnabled) { Cancel(); return; }
        if (Time.timeScale <= 0f) return;
        elapsed += Time.deltaTime;
        if (elapsed < warningTime)
        {
            material.color = new Color(1f,.2f,.28f,.5f + .35f * Mathf.Sin(elapsed * 19f));
            return;
        }
        float phase = elapsed - warningTime;
        if (phase >= .8f) { Cancel(); return; }
        float height = phase < .12f ? Mathf.Lerp(.045f,1f,phase/.12f) : phase < .45f ? 1f : 1f-(phase-.45f)/.35f;
        transform.localScale = new Vector3(1f,height,1f);
        material.color = new Color(1f,1f,1f,Mathf.Clamp01((.8f-phase)/.2f));
        if (!IsActive || struck) return;
        int count = Physics2D.OverlapBox((Vector2)transform.position + Vector2.up*.7f, new Vector2(1.05f,1.35f),0,
            new ContactFilter2D { useTriggers = false },overlaps);
        for (int i=0;i<count;i++)
        {
            var player=overlaps[i].GetComponentInParent<Movement>();
            if(player==null || !player.isActiveAndEnabled)continue;
            var health=player.GetComponent<PlayerHealth>();
            if(health==null || health.IsDead)continue;
            health.TakeDamage(damage,Vector2.up,.8f);struck=true;break;
        }
    }
    public void Cancel() { gameObject.SetActive(false); Destroy(gameObject); }
    private void OnDestroy() { if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material); }
}
