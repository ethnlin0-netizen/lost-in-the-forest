using System.Collections.Generic;
using UnityEngine;

// Navigation for static, axis-aligned BoxCollider2D platforms. Nodes are feet
// positions, so every graph is checked against the requesting actor's dimensions.
public class PlatformNavigation2D : MonoBehaviour
{
    public enum LinkType { Walk, Jump, Drop }
    public struct Node { public Vector2 feet; public BoxCollider2D surface; }
    public struct Link
    {
        public int from, to;
        public LinkType type;
        public float velocityX, duration, horizontalDelay, cost;
    }
    public class Graph
    {
        public readonly List<Node> nodes = new List<Node>();
        public readonly List<Link>[] edges;
        public int revision;
        public Graph(List<Node> points, int version)
        {
            nodes.AddRange(points); revision = version;
            edges = new List<Link>[nodes.Count];
            for (int i = 0; i < edges.Length; i++) edges[i] = new List<Link>();
        }
    }
    [SerializeField] private Transform surfaceRoot;
    [SerializeField, Min(0.3f)] private float nodeSpacing = 0.8f;
    [SerializeField, Min(0.1f)] private float geometryCheckInterval = 0.5f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private bool showConnections;
    private readonly List<BoxCollider2D> surfaces = new List<BoxCollider2D>();
    private readonly List<Bounds> lastBounds = new List<Bounds>();
    private readonly RaycastHit2D[] hits = new RaycastHit2D[64];
    private readonly Collider2D[] overlaps = new Collider2D[64];
    private float checkRemaining;
    private Graph debugGraph;
    public int Revision { get; private set; }
    public IReadOnlyList<BoxCollider2D> Surfaces => surfaces;
    private const float Skin = 0.04f;

    private void Awake() { RebuildSurfaces(); }
    private void Update()
    {
        checkRemaining -= Time.deltaTime;
        if (checkRemaining > 0f) return;
        checkRemaining = geometryCheckInterval;
        var found = (surfaceRoot != null ? surfaceRoot : transform).GetComponentsInChildren<BoxCollider2D>();
        int count = 0;
        bool changed = false;
        foreach (var c in found)
        {
            if (!IsObstacle(c)) continue;
            if (count >= surfaces.Count || surfaces[count] != c || lastBounds[count] != c.bounds) changed = true;
            count++;
        }
        if (changed || count != surfaces.Count) RebuildSurfaces();
    }
    public void RebuildSurfaces()
    {
        surfaces.Clear(); lastBounds.Clear();
        foreach (var c in (surfaceRoot != null ? surfaceRoot : transform).GetComponentsInChildren<BoxCollider2D>())
            if (IsObstacle(c)) { surfaces.Add(c); lastBounds.Add(c.bounds); }
        Revision++;
    }
    private ContactFilter2D Filter()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(obstacleLayers); return filter;
    }
    private bool IsObstacle(Collider2D c)
    {
        return c != null && c.enabled && c.gameObject.activeInHierarchy && !c.isTrigger &&
            (obstacleLayers.value & (1 << c.gameObject.layer)) != 0 &&
            c.GetComponentInParent<Movement>() == null && c.GetComponentInParent<BlightEnemy>() == null &&
            (c.attachedRigidbody == null || c.attachedRigidbody.bodyType == RigidbodyType2D.Static);
    }
    public BoxCollider2D SupportAt(Vector2 feet, float distance = 0.2f)
    {
        int count = Physics2D.Raycast(feet + Vector2.up * 0.08f, Vector2.down, Filter(), hits, distance + 0.08f);
        BoxCollider2D best = null; float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
            if (hits[i].normal.y > 0.5f && IsObstacle(hits[i].collider) && hits[i].distance < nearest)
            { best = hits[i].collider as BoxCollider2D; nearest = hits[i].distance; }
        return best;
    }
    private bool Clear(Vector2 feet, Vector2 size)
    {
        int count = Physics2D.OverlapBox(feet + Vector2.up * (size.y * 0.5f), size - Vector2.one * Skin, 0f, Filter(), overlaps);
        for (int i = 0; i < count; i++) if (IsObstacle(overlaps[i])) return false;
        return true;
    }
    public bool ClearSegment(Vector2 from, Vector2 to, Vector2 size)
    {
        Vector2 delta = to - from;
        int count = Physics2D.BoxCast(from + Vector2.up * size.y * 0.5f, size - Vector2.one * Skin,
            0f, delta.normalized, Filter(), hits, delta.magnitude);
        for (int i = 0; i < count; i++) if (IsObstacle(hits[i].collider)) return false;
        return true;
    }
    public Graph BuildGraph(Vector2 size, float speed, float jumpSpeed, float gravity, float maxDrop, float airSpeed = 0f)
    {
        var points = new List<Node>();
        foreach (var surface in surfaces)
        {
            if (!IsObstacle(surface)) continue;
            Bounds b = surface.bounds;
            float left = b.min.x + size.x * 0.5f + Skin;
            float right = b.max.x - size.x * 0.5f - Skin;
            if (right < left) continue;
            int intervals = Mathf.Max(1, Mathf.CeilToInt((right - left) / nodeSpacing));
            var samples = new List<float>();
            for (int i = 0; i <= intervals; i++) samples.Add(Mathf.Lerp(left, right, (float)i / intervals));
            // Put takeoff nodes beside risers, rather than relying on a coarse grid
            // to happen to leave enough horizontal room for a broad, slow enemy.
            foreach (var other in surfaces)
            {
                if (other == surface || !IsObstacle(other)) continue;
                float before = other.bounds.min.x - size.x * 0.5f - Skin * 2f;
                float after = other.bounds.max.x + size.x * 0.5f + Skin * 2f;
                if (before >= left && before <= right) samples.Add(before);
                if (after >= left && after <= right) samples.Add(after);
            }
            samples.Sort(); float lastSample = float.NegativeInfinity;
            foreach (float x in samples)
            {
                if (x - lastSample < 0.03f) continue;
                lastSample = x;
                var feet = new Vector2(x, b.max.y + Skin);
                if (Clear(feet, size)) points.Add(new Node { feet = feet, surface = surface });
            }
        }
        var graph = new Graph(points, Revision);
        speed = Mathf.Max(0.1f, speed); gravity = Mathf.Max(0.1f, gravity);
        float jumpHorizontalSpeed = airSpeed > 0f ? airSpeed : speed;
        float maxFlight = jumpSpeed * 2f / gravity + Mathf.Sqrt(2f * maxDrop / gravity);
        for (int i = 0; i < points.Count; i++)
        {
            for (int j = 0; j < points.Count; j++)
            {
                if (i == j) continue;
                Node a = points[i], b = points[j]; Vector2 delta = b.feet - a.feet;
                if (a.surface == b.surface && Mathf.Abs(delta.x) <= nodeSpacing * 1.1f && ClearSegment(a.feet, b.feet, size))
                    graph.edges[i].Add(new Link { from = i, to = j, type = LinkType.Walk, cost = Mathf.Abs(delta.x) / speed });
                if (jumpSpeed <= 0f || Mathf.Abs(delta.x) > jumpHorizontalSpeed * maxFlight || delta.y < -maxDrop) continue;
                // Jumps also cross holes or a blocked walking segment on the same surface.
                if (a.surface == b.surface && ClearSegment(a.feet, b.feet, size)) continue;
                if (TryJump(a, b, size, jumpHorizontalSpeed, jumpSpeed, gravity, maxDrop, out float vx, out float duration, out float delay))
                    graph.edges[i].Add(new Link { from = i, to = j, type = LinkType.Jump, velocityX = vx, duration = duration, horizontalDelay = delay, cost = duration + 0.35f });
            }
            // Drops start at a platform's outer node, walking beyond its lip before falling.
            foreach (int direction in new[] { -1, 1 })
            {
                Bounds bounds = points[i].surface.bounds;
                float lip = direction < 0 ? bounds.min.x : bounds.max.x;
                if (Mathf.Abs(points[i].feet.x - lip) > size.x * 0.5f + Skin + 0.01f) continue;
                if (TryDrop(points[i], direction, size, speed, gravity, maxDrop, out BoxCollider2D landing, out Vector2 feet, out float duration))
                {
                    int closest = Nearest(graph, feet, landing);
                    if (closest >= 0) graph.edges[i].Add(new Link { from = i, to = closest, type = LinkType.Drop,
                        velocityX = speed * direction, duration = duration, cost = duration + 0.2f });
                }
            }
        }
        debugGraph = graph;
        return graph;
    }
    public bool TryJump(Node from, Node to, Vector2 size, float speed, float jumpSpeed, float gravity,
        float maxDrop, out float vx, out float duration, out float horizontalDelay)
    {
        vx = duration = horizontalDelay = 0f;
        float dy = to.feet.y - from.feet.y;
        // Semi-implicit Rigidbody2D integration lowers the apex slightly versus the continuous parabola.
        float effectiveJump = jumpSpeed - gravity * Time.fixedDeltaTime * 0.5f;
        float discriminant = effectiveJump * effectiveJump - 2f * gravity * dy;
        if (dy < -maxDrop || discriminant <= 0f || jumpSpeed <= 0f) return false;
        float time = (effectiveJump + Mathf.Sqrt(discriminant)) / gravity;
        float horizontal = (to.feet.x - from.feet.x) / time;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            float delay = 0f;
            if (attempt == 1)
            {
                if (dy <= 0f) continue;
                // Lift clear of a nearby riser before steering across it.
                delay = (effectiveJump - Mathf.Sqrt(Mathf.Max(0f, effectiveJump * effectiveJump - 2f * gravity * (dy + Skin)))) / gravity + 0.04f;
            }
            horizontal = (to.feet.x - from.feet.x) / Mathf.Max(0.01f, time - delay);
            if (Mathf.Abs(horizontal) > speed) continue;
            if (!TraceFlight(from.feet, size, new Vector2(horizontal, jumpSpeed), gravity, from.surface, false,
                maxDrop, out BoxCollider2D surface, out Vector2 end, out float elapsed, delay)) continue;
            if (surface != to.surface || Mathf.Abs(end.x - to.feet.x) > 0.2f) continue;
            vx = horizontal; duration = elapsed; horizontalDelay = delay; return true;
        }
        return false;
    }
    private bool TryDrop(Node from, int direction, Vector2 size, float speed, float gravity, float maxDrop,
        out BoxCollider2D surface, out Vector2 end, out float duration)
    {
        float lip = direction < 0 ? from.surface.bounds.min.x : from.surface.bounds.max.x;
        // Full-body clearance before predicting free fall is conservative for a box character.
        Vector2 leave = new Vector2(lip + direction * (size.x * 0.5f + Skin), from.feet.y);
        surface = null; end = default; duration = 0f;
        if (!ClearSegment(from.feet, leave, size)) return false;
        if (!TraceFlight(leave, size, Vector2.right * (direction * speed), gravity, from.surface, true,
            maxDrop, out surface, out end, out duration)) return false;
        duration += Mathf.Abs(leave.x - from.feet.x) / speed;
        return surface != from.surface && from.feet.y - end.y <= maxDrop + 0.1f;
    }
    private bool TraceFlight(Vector2 start, Vector2 size, Vector2 velocity, float gravity, BoxCollider2D source,
        bool drop, float maxDrop, out BoxCollider2D landing, out Vector2 end, out float duration, float horizontalDelay = 0f)
    {
        landing = null; end = start; duration = 0f;
        float step = Time.fixedDeltaTime;
        for (int tick = 0; tick < Mathf.CeilToInt(4f / step); tick++)
        {
            velocity.y -= gravity * step;
            Vector2 delta = velocity * step;
            if (duration < horizontalDelay) delta.x = 0f;
            int count = Physics2D.BoxCast(end + Vector2.up * size.y * 0.5f, size - Vector2.one * Skin,
                0f, delta.normalized, Filter(), hits, delta.magnitude);
            int closest = -1; float distance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (!IsObstacle(hits[i].collider)) continue;
                if (drop && hits[i].collider == source) continue;
                if (hits[i].distance < distance) { closest = i; distance = hits[i].distance; }
            }
            duration += step;
            if (closest >= 0)
            {
                var hit = hits[closest]; landing = hit.collider as BoxCollider2D;
                end += delta.normalized * distance;
                if (velocity.y >= 0f || hit.normal.y < 0.5f || landing == null) return false;
                Bounds b = landing.bounds;
                // BoxCast uses a slightly inset body. Permit its skin-width contact
                // offset while still requiring essentially the whole body on the top.
                if (end.x < b.min.x + size.x * 0.5f - Skin * 0.5f || end.x > b.max.x - size.x * 0.5f + Skin * 0.5f) return false;
                end.y = b.max.y + Skin;
                return start.y - end.y <= maxDrop + 0.1f;
            }
            end += delta;
            if (end.y < start.y - maxDrop - 0.2f) return false;
        }
        return false;
    }
    public static int Nearest(Graph graph, Vector2 feet, BoxCollider2D surface = null)
    {
        int result = -1; float best = float.PositiveInfinity;
        for (int i = 0; i < graph.nodes.Count; i++)
        {
            if (surface != null && graph.nodes[i].surface != surface) continue;
            float score = (graph.nodes[i].feet - feet).sqrMagnitude;
            if (score < best) { best = score; result = i; }
        }
        return result;
    }
    // Dijkstra returns the closest reachable alternative when the goal has no route.
    public static bool FindRoute(Graph graph, int start, int goal, List<Link> route)
    {
        route.Clear();
        if (start < 0 || goal < 0) return false;
        int count = graph.nodes.Count;
        var cost = new float[count]; var visited = new bool[count]; var previous = new Link[count];
        for (int i = 0; i < count; i++) { cost[i] = float.PositiveInfinity; previous[i].from = -1; }
        cost[start] = 0f;
        for (int iteration = 0; iteration < count; iteration++)
        {
            int current = -1; float lowest = float.PositiveInfinity;
            for (int i = 0; i < count; i++) if (!visited[i] && cost[i] < lowest) { current = i; lowest = cost[i]; }
            if (current < 0) break;
            visited[current] = true;
            foreach (var link in graph.edges[current])
                if (cost[current] + link.cost < cost[link.to]) { cost[link.to] = cost[current] + link.cost; previous[link.to] = link; }
        }
        bool reachable = visited[goal]; int end = goal;
        if (!reachable)
        {
            float best = float.PositiveInfinity; end = start;
            for (int i = 0; i < count; i++) if (visited[i])
            {
                float distance = (graph.nodes[i].feet - graph.nodes[goal].feet).sqrMagnitude;
                if (distance < best) { best = distance; end = i; }
            }
        }
        while (end != start && previous[end].from >= 0) { var link = previous[end]; route.Add(link); end = link.from; }
        route.Reverse(); return reachable;
    }
    private void OnDrawGizmosSelected()
    {
        if (!showConnections || debugGraph == null) return;
        foreach (var links in debugGraph.edges) foreach (var link in links)
        {
            Gizmos.color = link.type == LinkType.Walk ? Color.green : link.type == LinkType.Jump ? Color.cyan : Color.yellow;
            Gizmos.DrawLine(debugGraph.nodes[link.from].feet, debugGraph.nodes[link.to].feet);
        }
    }
}
