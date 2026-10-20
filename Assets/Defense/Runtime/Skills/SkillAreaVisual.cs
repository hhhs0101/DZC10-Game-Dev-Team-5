using UnityEngine;
namespace Defense {
// Replaceable placeholder presentation; removing this component never changes the effect.
[RequireComponent(typeof(LineRenderer))]
public sealed class SkillAreaVisual : MonoBehaviour {
    private LineRenderer line;
    private Material material;
    private PersistentSlowArea area;
    public void Observe(PersistentSlowArea runtime) {
        area = runtime; area.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        if (area != null) Show(area.transform.position,area.Radius,new Color(.5f,.85f,1,area.VisualOpacity),area.Plane);
    }
    public void Show(Vector3 center, float radius, Color color, WaypointPlane plane=WaypointPlane.XZ) {
        if (line == null) {
            line = GetComponent<LineRenderer>(); line.useWorldSpace = true;
            line.loop = true; line.positionCount = 96; line.widthMultiplier = .045f;
            material = new Material(Shader.Find("Sprites/Default")); line.sharedMaterial = material;
        }
        line.enabled = isActiveAndEnabled && color.a > 0;
        line.startColor = line.endColor = color;
        line.sortingOrder = plane == WaypointPlane.XY ? 30 : 0;
        for (int i=0;i<line.positionCount;i++) {
            float angle = i*Mathf.PI*2/line.positionCount;
            line.SetPosition(i,center+PlanarSpace.World(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,plane == WaypointPlane.XY ? 0 : .06f,plane));
        }
    }
    public void Hide() { if (line != null) line.enabled = false; }
    private void OnEnable() => Refresh();
    private void OnDisable() => Hide();
    private void OnDestroy() {
        if (area != null) area.Changed -= Refresh;
        if (material != null) Destroy(material);
    }
}
}
