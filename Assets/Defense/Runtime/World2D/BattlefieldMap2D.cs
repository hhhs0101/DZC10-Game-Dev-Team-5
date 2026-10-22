using UnityEngine;
namespace Defense {
// Scene configuration only. Stage/session routing and all balance data remain in existing services.
public sealed class BattlefieldMap2D : MonoBehaviour {
    [SerializeField] private Camera worldCamera;
    [SerializeField] private PlayableArea2D playableArea;
    [SerializeField] private WaypointPath path;
    [SerializeField, Tooltip("Optional multi-branch routes; defaults to primary path when empty.")] private WaypointPath[] branchPaths;
    [SerializeField] private TowerPlacementSlot[] placementSlots;
    [SerializeField] private Transform enemySpawnRoot;
    [SerializeField, Tooltip("Editable Base anchor; also the final waypoint on this map.")] private Transform baseDestination;
    [SerializeField] private Transform enemyRuntimeRoot;
    [SerializeField] private Transform towerRuntimeRoot;
    [SerializeField] private Transform skillEffectRoot;
    public Camera WorldCamera => worldCamera;
    public PlayableArea2D PlayableArea => playableArea;
    public WaypointPath Path => path;
    public System.Collections.Generic.IReadOnlyList<WaypointPath> BranchPaths => branchPaths != null && branchPaths.Length > 0 ? branchPaths : (path != null ? new[] { path } : System.Array.Empty<WaypointPath>());
    public System.Collections.Generic.IReadOnlyList<TowerPlacementSlot> PlacementSlots => placementSlots;
    public Transform EnemySpawnRoot => enemySpawnRoot;
    public Transform BaseDestination => baseDestination;
    public Transform EnemyRuntimeRoot => enemyRuntimeRoot;
    public Transform TowerRuntimeRoot => towerRuntimeRoot;
    public Transform SkillEffectRoot => skillEffectRoot;
}
}
