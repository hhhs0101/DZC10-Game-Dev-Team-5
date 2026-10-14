using UnityEngine;
namespace Defense {
[RequireComponent(typeof(BoxCollider))]
public sealed class TabletopSurface : MonoBehaviour {
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask surfaceLayers;
    [SerializeField] private LayerMask placementLayers;
    [SerializeField] private LayerMask enemyLayers;
    public Camera WorldCamera => worldCamera;
    public LayerMask SurfaceLayers => surfaceLayers;
    public LayerMask PlacementLayers => placementLayers;
    public LayerMask EnemyLayers => enemyLayers;
    public float Height => GetComponent<BoxCollider>().bounds.max.y;
    public bool Contains(Vector2 point) {
        var bounds = GetComponent<BoxCollider>().bounds;
        return point.x >= bounds.min.x && point.x <= bounds.max.x && point.y >= bounds.min.z && point.y <= bounds.max.z;
    }
}
}
