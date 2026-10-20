using UnityEngine;
namespace Defense {
// Stage 1 diagnostic only: not a combat Enemy or a second stage/session system.
[RequireComponent(typeof(EnemyPathFollower))]
public sealed class PathMovementSample2D : MonoBehaviour {
    [SerializeField] private WaypointPath path;
    [SerializeField, Tooltip("Read existing movement speed without changing EnemyDefinition or its combat prefab.")] private EnemyDefinition definition;
    public bool ReachedEnd { get; private set; }
    private EnemyPathFollower follower;
    private void Start() {
        follower = GetComponent<EnemyPathFollower>();
        if (path == null || path.Count < 2 || path.Plane != WaypointPlane.XY || definition == null) {
            Debug.LogError("2D movement sample requires an XY route with at least two points and an Enemy definition.",this); enabled = false; return;
        }
        follower.ReachedEnd += OnReachedEnd;
        follower.Initialize(path,definition.MovementSpeed);
    }
    private void OnReachedEnd() => ReachedEnd = true;
    private void OnDestroy() { if (follower != null) follower.ReachedEnd -= OnReachedEnd; }
}
}
