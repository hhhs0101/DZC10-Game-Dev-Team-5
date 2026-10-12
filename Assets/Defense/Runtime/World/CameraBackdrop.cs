using UnityEngine;
namespace Defense {
// Presentation only: a camera-facing quad behind the battlefield, with aspect-fill cropping.
[ExecuteAlways, RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public sealed class CameraBackdrop : MonoBehaviour {
    [SerializeField, Tooltip("Camera that owns this background quad.")] private Camera worldCamera;
    [SerializeField, Range(.5f,.99f), Tooltip("Position as a fraction of the camera far clipping distance.")] private float depthFraction = .9f;
    private void OnEnable() => Refresh();
    private void LateUpdate() => Refresh();
    private void OnValidate() => Refresh();
    public void Refresh() {
        if (worldCamera == null) return;
        var material = GetComponent<MeshRenderer>().sharedMaterial;
        var texture = material != null ? material.mainTexture : null;
        if (texture == null) return;
        float distance = Mathf.Lerp(worldCamera.nearClipPlane,worldCamera.farClipPlane,depthFraction);
        float height = worldCamera.orthographic ? 2*worldCamera.orthographicSize : 2*distance*Mathf.Tan(worldCamera.fieldOfView*.5f*Mathf.Deg2Rad);
        float aspect = (float)texture.width/texture.height;
        height *= Mathf.Max(1,worldCamera.aspect/aspect);
        transform.SetPositionAndRotation(worldCamera.transform.position+worldCamera.transform.forward*distance,worldCamera.transform.rotation);
        transform.localScale = new Vector3(height*aspect,height,1);
    }
}
}
