using UnityEngine;
namespace Defense {
[ExecuteAlways]
public sealed class PlacementSlotView : MonoBehaviour {
    [SerializeField] private TowerPlacementSlot slot;
    [SerializeField] private Renderer appearance;
    [SerializeField] private Color available = new Color(.2f,.45f,.3f);
    [SerializeField] private Color highlighted = new Color(.45f,.85f,.5f);
    [SerializeField] private Color occupied = new Color(.45f,.36f,.22f);
    private MaterialPropertyBlock properties;
    public Color CurrentColor { get; private set; }
    private void OnEnable() { if(slot != null) slot.Changed += Refresh; if(appearance != null) appearance.enabled = true; Refresh(); }
    private void OnDisable() { if(slot != null) slot.Changed -= Refresh; if(appearance != null) appearance.enabled = false; }
    public void Refresh() {
        if(slot == null || appearance == null) return;
        CurrentColor = slot.IsOccupied ? occupied : slot.IsHovered ? highlighted : available;
        properties = properties ?? new MaterialPropertyBlock();
        appearance.GetPropertyBlock(properties); properties.SetColor("_Color",CurrentColor); appearance.SetPropertyBlock(properties);
    }
}
}
