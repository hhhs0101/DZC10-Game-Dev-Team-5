using UnityEngine;
namespace Defense {
[RequireComponent(typeof(LineRenderer))]
[ExecuteAlways]
public sealed class PathVisualization : MonoBehaviour {
    [SerializeField] private WaypointPath path;
    [SerializeField] private float surfaceOffset = .035f;
    private LineRenderer line;
    private void Awake() => line = GetComponent<LineRenderer>();
    private void OnEnable() { line = GetComponent<LineRenderer>(); line.enabled = true; Refresh(); }
    private void LateUpdate() => Refresh();
    public void Refresh() {
        if (path == null) return;
        if (line == null) line = GetComponent<LineRenderer>();
        line.positionCount = path.Count;
        for(int i=0;i<path.Count;i++) line.SetPosition(i,path.GetPoint(i)+Vector3.up*surfaceOffset);
    }
    private void OnDisable() { if(line != null) line.enabled = false; }
}
}
