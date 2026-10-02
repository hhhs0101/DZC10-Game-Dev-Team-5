using UnityEngine;
namespace Defense {
public sealed class WaypointPath : MonoBehaviour {
    [SerializeField, Tooltip("Ordered from spawn to Base; at least two points.")] private Transform[] points;
    public int Count => points == null ? 0 : points.Length;
    public Vector3 GetPoint(int index) => new Vector3(points[index].position.x,transform.position.y,points[index].position.z);
    private void OnDrawGizmos() {
        if (points == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 1; i < points.Length; i++)
            if (points[i-1] != null && points[i] != null) Gizmos.DrawLine(points[i-1].position, points[i].position);
    }
}
}
