using UnityEngine;
namespace Defense {
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class TowerPlacementSlot : MonoBehaviour {
    [SerializeField] private Color hoveredColor = new Color(.45f,.85f,.5f);
    [SerializeField] private Color occupiedColor = new Color(.45f,.36f,.22f);
    private SpriteRenderer appearance;
    private Color availableColor;
    private bool hovered;
    public Turret Occupant { get; private set; }
    public bool IsOccupied => Occupant != null;
    private void Awake() {
        appearance = GetComponent<SpriteRenderer>(); availableColor = appearance.color;
    }
    public void SetHovered(bool value) { hovered = value; RefreshAppearance(); }
    private void RefreshAppearance() {
        appearance.color = IsOccupied ? occupiedColor : hovered ? hoveredColor : availableColor;
    }
    public bool TryOccupy(Turret turret) {
        if (IsOccupied || turret == null) return false;
        Occupant = turret; RefreshAppearance(); return true;
    }
}
}
