using UnityEngine;
namespace Defense {
[ExecuteAlways, RequireComponent(typeof(Camera))]
public sealed class TopDownCameraFrame : MonoBehaviour {
    [SerializeField] private PlayableArea2D playableArea;
    [SerializeField, Tooltip("Optional full-map artwork to frame instead of the logical floor shape.")] private SpriteRenderer framingVisual;
    [SerializeField, Min(0), Tooltip("Minimum vertical half-size; expands when necessary to show the map.")] private float minimumSize = 0;
    [SerializeField, Min(0), Tooltip("World-space margin around the logical playable area.")] private float padding = 0;
    [SerializeField, Range(0f, .4f), Tooltip("Fraction of the screen width reserved on the right for the HUD sidebar.")] private float rightSidebarFraction = 220f / 1280f;
    [SerializeField, Min(.1f)] private float depth = 10;
    private void OnEnable() => Refresh();
    private void OnValidate() => Refresh();
    private void LateUpdate() => Refresh();
    public void Refresh() {
        if (playableArea == null || playableArea.Shape == null) return;
        var camera = GetComponent<Camera>(); var bounds = framingVisual != null ? framingVisual.bounds : playableArea.Bounds;
        camera.orthographic = true;
        float aspect = Mathf.Max(.01f, camera.aspect);
        float mapWidthFraction = Mathf.Max(.2f, 1f - rightSidebarFraction);
        if (framingVisual != null) {
            camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x / (mapWidthFraction * aspect));
        } else {
            camera.orthographicSize = Mathf.Max(minimumSize, bounds.extents.y + padding, (bounds.extents.x + padding) / (mapWidthFraction * aspect));
        }
        float horizontalShift = camera.orthographicSize * aspect * rightSidebarFraction;
        float verticalShift = framingVisual != null ? Mathf.Max(0f, (camera.orthographicSize - bounds.extents.y) * .3f) : 0f;
        transform.SetPositionAndRotation(new Vector3(bounds.center.x + horizontalShift, bounds.center.y + verticalShift, bounds.center.z - depth), Quaternion.identity);
    }
}
}
