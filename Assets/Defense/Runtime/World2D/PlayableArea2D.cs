using UnityEngine;
namespace Defense {
// Explicit gameplay shape, independent of the tabletop image. Any Collider2D outline can be used.
public sealed class PlayableArea2D : MonoBehaviour {
    [SerializeField, Tooltip("Queryable map shape; BoxCollider2D may be replaced by PolygonCollider2D, etc.")] private Collider2D shape;
    public Collider2D Shape => shape;
    public Bounds Bounds => shape.bounds;
    public bool Contains(Vector2 worldPoint) => shape != null && shape.enabled && shape.gameObject.activeInHierarchy && shape.OverlapPoint(worldPoint);
}
}
