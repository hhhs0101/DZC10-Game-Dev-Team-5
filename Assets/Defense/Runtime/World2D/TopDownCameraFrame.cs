using UnityEngine;
namespace Defense {
[ExecuteAlways, RequireComponent(typeof(Camera))]
public sealed class TopDownCameraFrame : MonoBehaviour {
    [SerializeField] private PlayableArea2D playableArea;
    [SerializeField, Tooltip("Optional full-map artwork to frame instead of the logical floor shape.")] private SpriteRenderer framingVisual;
    [SerializeField, Min(.01f), Tooltip("Minimum vertical half-size; expands when necessary to show the map.")] private float minimumSize = 6;
    [SerializeField, Min(0), Tooltip("World-space margin around the logical playable area.")] private float padding = .75f;
    [SerializeField, Min(.1f)] private float depth = 10;
    private void OnEnable() => Refresh();
    private void OnValidate() => Refresh();
    private void LateUpdate() => Refresh();
    public void Refresh() {
        if (playableArea == null || playableArea.Shape == null) return;
        var camera = GetComponent<Camera>(); var bounds = framingVisual != null ? framingVisual.bounds : playableArea.Bounds;
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(minimumSize,bounds.extents.y+padding,(bounds.extents.x+padding)/Mathf.Max(.01f,camera.aspect));
        transform.SetPositionAndRotation(new Vector3(bounds.center.x,bounds.center.y,bounds.center.z-depth),Quaternion.identity);
    }
}
}
