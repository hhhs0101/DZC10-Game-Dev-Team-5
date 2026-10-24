using UnityEngine;
namespace Defense {
public enum WaypointPlane { XZ = 0, XY = 1 }
public sealed class WaypointPath : MonoBehaviour {
    [SerializeField, Tooltip("Projection plane; current battlefields use XY. Serialized enum values remain stable.")] private WaypointPlane plane;
    public WaypointPlane Plane => plane;
    public Vector3 Normal => plane == WaypointPlane.XY ? Vector3.forward : Vector3.up;
    public Vector3 Constrain(Vector3 point) => plane == WaypointPlane.XY
        ? new Vector3(point.x,point.y,transform.position.z)
        : new Vector3(point.x,transform.position.y,point.z);
    [SerializeField, Tooltip("Ordered from spawn to Base; at least two points.")] private Transform[] points;
    public int Count => points == null ? 0 : points.Length;
    public Vector3 GetPoint(int index) => Constrain(points[index].position);
    public void Configure(WaypointPlane waypointPlane, Transform[] waypoints) {
        plane = waypointPlane;
        points = waypoints;
    }
    private void OnDrawGizmos() {
        if (points == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 1; i < points.Length; i++)
            if (points[i-1] != null && points[i] != null) Gizmos.DrawLine(points[i-1].position, points[i].position);
    }
}
}
