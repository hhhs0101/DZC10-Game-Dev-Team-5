using UnityEngine;
namespace Defense {
// Observes common slot state. No placement, input or combat behavior.
[ExecuteAlways]
public sealed class PlacementSlotSpriteView : MonoBehaviour {
    [SerializeField] private TowerPlacementSlot slot;
    [SerializeField] private SpriteRenderer appearance;
    [SerializeField] private Color available = new Color(.25f,.65f,.5f);
    [SerializeField] private Color highlighted = new Color(.75f,1,.4f);
    [SerializeField] private Color occupied = new Color(.45f,.35f,.25f);
    public Color CurrentColor { get; private set; }
    private void OnEnable() { if(slot != null) slot.Changed += Refresh; if(appearance != null) appearance.enabled = true; Refresh(); }
    private void OnDisable() { if(slot != null) slot.Changed -= Refresh; if(appearance != null) appearance.enabled = false; }
    private void OnValidate() => Refresh();
    public void Refresh() {
        if(slot == null || appearance == null) return;
        CurrentColor = slot.IsOccupied ? occupied : slot.IsHovered ? highlighted : available;
        appearance.color = CurrentColor;
    }
}
}
