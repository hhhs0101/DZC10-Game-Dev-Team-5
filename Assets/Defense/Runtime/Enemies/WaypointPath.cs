using UnityEngine;
namespace Defense {
public sealed class WaypointPath : MonoBehaviour {
    [SerializeField, Tooltip("Ordered from spawn to Base; at least two points.")] private Transform[] points;
    public int Count => points == null ? 0 : points.Length;
    public Vector3 GetPoint(int index) => points[index].position;
    private void OnDrawGizmos() {
        if (points == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 1; i < points.Length; i++)
            if (points[i-1] != null && points[i] != null) Gizmos.DrawLine(points[i-1].position, points[i].position);
    }
}
}
