using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class EnemyNavigator2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float jumpSpeed = 10f;
    [SerializeField, Range(0.5f, 2f)] private float jumpHorizontalSpeedMultiplier = 1f;
    [SerializeField, Min(0f)] private float maximumDropDistance = 4f;
    [SerializeField, Min(0.1f)] private float routeRefreshInterval = 0.35f;
    [SerializeField, Min(0.2f)] private float stuckTimeout = 1f;
    [SerializeField] private bool showRoute = true;
    private PlatformNavigation2D world;
    private PlatformNavigation2D.Graph graph;
    private Rigidbody2D body;
    private BoxCollider2D shape;
    private readonly List<PlatformNavigation2D.Link> route = new List<PlatformNavigation2D.Link>();
    private Vector2 destination;
    private int routeIndex;
    private float refreshRemaining, traversalElapsed, traversalVelocity, traversalLimit, traversalDelay;
    private bool hasDestination, traversing, leftGround;
    private float stuckElapsed, retryRemaining;
    private Vector2 lastPosition;
    private BoxCollider2D lastGoalSupport;
    private int lastPlannedGoal = -1;
    private bool routeNeedsRefresh = true;
    private float graphSpeed, graphJump, graphGravity, graphDrop;
    private float graphAirMultiplier;
    private Vector2 graphSize;
    public bool GoalReachable { get; private set; }
    public bool IsTraversing => traversing;
    public int RoutesCalculated { get; private set; }
    public int JumpsPerformed { get; private set; }
    public int DropsPerformed { get; private set; }
    public int StuckRecoveries { get; private set; }
    public int RemainingLinks => Mathf.Max(0, route.Count - routeIndex);
    public Vector2 Feet => new Vector2(shape.bounds.center.x, shape.bounds.min.y);
    public bool IsGrounded => world != null && world.SupportAt(Feet, 0.06f) != null && body.linearVelocity.y <= 0.1f;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); shape = GetComponent<BoxCollider2D>();
        world = FindAnyObjectByType<PlatformNavigation2D>();
    }
    public void SetDestination(Vector2 feet)
    {
        destination = feet; hasDestination = true;
    }
    public void Stop()
    {
        hasDestination = traversing = leftGround = false;
        route.Clear(); routeIndex = 0; lastGoalSupport = null; refreshRemaining = 0f;
        lastPlannedGoal = -1; routeNeedsRefresh = true; stuckElapsed = 0f;
    }
    // A knockback invalidates the planned takeoff. Let the impulse finish before replanning.
    public void Interrupt() { Stop(); graph = null; }
    public float Tick(float deltaTime, float speed)
    {
        if (world == null) world = FindAnyObjectByType<PlatformNavigation2D>();
        if (world == null || !hasDestination || speed <= 0f) return 0f;
        refreshRemaining -= deltaTime; retryRemaining -= deltaTime;
        if (traversing)
        {
            traversalElapsed += deltaTime;
            if (!IsGrounded) leftGround = true;
            if (leftGround && IsGrounded && traversalElapsed > 0.08f)
            {
                traversing = false; routeIndex++; refreshRemaining = 0f; routeNeedsRefresh = true;
            }
            else if (traversalElapsed > traversalLimit)
            {
                traversing = false; route.Clear(); refreshRemaining = 0f;
                routeNeedsRefresh = true;
                retryRemaining = routeRefreshInterval; StuckRecoveries++;
            }
            else return traversalElapsed < traversalDelay ? 0f : traversalVelocity;
        }
        if (!IsGrounded) return 0f;
        if (retryRemaining > 0f) return 0f;
        float gravity = -Physics2D.gravity.y * body.gravityScale;
        Vector2 size = shape.bounds.size;
        if (graph == null || graph.revision != world.Revision || graphSize != size || graphSpeed != speed ||
            graphJump != jumpSpeed || graphGravity != gravity || graphDrop != maximumDropDistance || graphAirMultiplier != jumpHorizontalSpeedMultiplier)
        {
            graph = world.BuildGraph(size, speed, jumpSpeed, gravity, maximumDropDistance, speed * jumpHorizontalSpeedMultiplier);
            graphSize = size; graphSpeed = speed; graphJump = jumpSpeed; graphGravity = gravity; graphDrop = maximumDropDistance;
            graphAirMultiplier = jumpHorizontalSpeedMultiplier;
            refreshRemaining = 0f;
            routeNeedsRefresh = true;
        }
        var goalSupport = world.SupportAt(destination, 0.6f);
        int goal = PlatformNavigation2D.Nearest(graph, destination, goalSupport);
        if (routeNeedsRefresh || goalSupport != lastGoalSupport || (refreshRemaining <= 0f && goal != lastPlannedGoal))
        {
            int start = PlatformNavigation2D.Nearest(graph, Feet, world.SupportAt(Feet));
            GoalReachable = PlatformNavigation2D.FindRoute(graph, start, goal, route);
            MergeWalkingLinks();
            routeIndex = 0; lastGoalSupport = goalSupport; refreshRemaining = routeRefreshInterval; RoutesCalculated++;
            lastPlannedGoal = goal; routeNeedsRefresh = false;
        }
        // A run of walking links is one continuous movement. Crossing a waypoint
        // must not insert a physics frame with zero velocity.
        while (routeIndex < route.Count && route[routeIndex].type == PlatformNavigation2D.LinkType.Walk &&
            Mathf.Abs(graph.nodes[route[routeIndex].to].feet.x - Feet.x) < 0.12f) routeIndex++;
        if (routeIndex >= route.Count)
        {
            var support = world.SupportAt(Feet);
            if (GoalReachable && support != null && goalSupport == support)
            {
                float finalX = Mathf.Clamp(destination.x, support.bounds.min.x + size.x * 0.5f + 0.08f,
                    support.bounds.max.x - size.x * 0.5f - 0.08f);
                bool needsMovement = Mathf.Abs(finalX - Feet.x) > 0.18f;
                float velocity = needsMovement && world.ClearSegment(Feet, new Vector2(finalX, Feet.y), size) ? SafeWalk(finalX, speed) : 0f;
                return TrackProgress(velocity, needsMovement, deltaTime);
            }
            stuckElapsed = 0f;
            return 0f;
        }
        var link = route[routeIndex];
        var from = graph.nodes[link.from]; var to = graph.nodes[link.to];
        float intended;
        if (world.SupportAt(Feet) != from.surface)
        { routeNeedsRefresh = true; return 0f; }
        if (link.type == PlatformNavigation2D.LinkType.Walk)
        {
            intended = SafeWalk(to.feet.x, speed);
        }
        else if (Mathf.Abs(from.feet.x - Feet.x) > 0.09f)
            intended = SafeWalk(from.feet.x, speed);
        else
        {
            float velocity = link.velocityX, duration = link.duration, delay = link.horizontalDelay;
            if (link.type == PlatformNavigation2D.LinkType.Jump)
            {
                var actual = from; actual.feet = new Vector2(Feet.x, from.feet.y);
                if (!world.TryJump(actual, to, size, speed * jumpHorizontalSpeedMultiplier, jumpSpeed, gravity, maximumDropDistance, out velocity, out duration, out delay))
                { graph = null; retryRemaining = routeRefreshInterval; return 0f; }
                body.linearVelocity = new Vector2(delay > 0f ? 0f : velocity, jumpSpeed); JumpsPerformed++;
            }
            else DropsPerformed++;
            traversing = true; leftGround = false; traversalElapsed = 0f;
            traversalVelocity = velocity; traversalLimit = duration + 0.75f;
            traversalDelay = delay;
            stuckElapsed = 0f; return delay > 0f ? 0f : velocity;
        }
        return TrackProgress(intended, Mathf.Abs(to.feet.x - Feet.x) > 0.18f, deltaTime);
    }
    private float TrackProgress(float intended, bool needsMovement, float deltaTime)
    {
        if (needsMovement)
        {
            stuckElapsed = Vector2.Distance(Feet, lastPosition) < 0.008f ? stuckElapsed + deltaTime : 0f;
            if (stuckElapsed > stuckTimeout)
            {
                graph = null; route.Clear(); retryRemaining = routeRefreshInterval;
                routeNeedsRefresh = true;
                stuckElapsed = 0f; StuckRecoveries++; intended = 0f;
            }
        }
        else stuckElapsed = 0f;
        lastPosition = Feet;
        return intended;
    }
    private void MergeWalkingLinks()
    {
        int write = 0;
        for (int read = 0; read < route.Count; read++)
        {
            var link = route[read];
            while (link.type == PlatformNavigation2D.LinkType.Walk && read + 1 < route.Count &&
                route[read + 1].type == PlatformNavigation2D.LinkType.Walk && route[read + 1].from == link.to &&
                graph.nodes[route[read + 1].to].surface == graph.nodes[link.from].surface)
            { var next = route[++read]; link.to = next.to; link.cost += next.cost; }
            route[write++] = link;
        }
        if (write < route.Count) route.RemoveRange(write, route.Count - write);
    }
    private float SafeWalk(float destinationX, float speed)
    {
        int direction = destinationX > Feet.x ? 1 : -1;
        Vector2 leading = Feet + Vector2.right * direction * (shape.bounds.extents.x + 0.12f);
        if (world.SupportAt(leading, 0.3f) == null || !world.ClearSegment(Feet, Feet + Vector2.right * direction * 0.15f, shape.bounds.size)) return 0f;
        return direction * Mathf.Min(speed, Mathf.Abs(destinationX - Feet.x) / Time.fixedDeltaTime);
    }
    private void OnDrawGizmosSelected()
    {
        if (!showRoute || graph == null || !hasDestination) return;
        Gizmos.color = GoalReachable ? Color.cyan : Color.yellow;
        for (int i = routeIndex; i < route.Count; i++)
            Gizmos.DrawLine(graph.nodes[route[i].from].feet, graph.nodes[route[i].to].feet);
        Gizmos.color = Color.white; Gizmos.DrawWireSphere(destination, 0.2f);
    }
}
