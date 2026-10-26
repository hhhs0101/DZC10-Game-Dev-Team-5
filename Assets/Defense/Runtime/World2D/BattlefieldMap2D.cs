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
    // Stage-specific runtime slots join the same placement/inspection list as scene slots.
    public void RegisterPlacementSlot(TowerPlacementSlot slot) {
        if(slot == null) return;
        if(placementSlots == null) placementSlots = System.Array.Empty<TowerPlacementSlot>();
        foreach(var existing in placementSlots) if(existing == slot) return;
        int index = placementSlots.Length;
        System.Array.Resize(ref placementSlots,index+1);
        placementSlots[index] = slot;
    }
    public Transform EnemySpawnRoot => enemySpawnRoot;
    public Transform BaseDestination => baseDestination;
    public Transform EnemyRuntimeRoot => enemyRuntimeRoot;
    public Transform TowerRuntimeRoot => towerRuntimeRoot;
    public Transform SkillEffectRoot => skillEffectRoot;
}
}
