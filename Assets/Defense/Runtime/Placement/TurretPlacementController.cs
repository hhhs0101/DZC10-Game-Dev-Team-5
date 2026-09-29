using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Defense {
[RequireComponent(typeof(GameplayHand))]
public sealed class TurretPlacementController : MonoBehaviour {
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameFlow flow;
    [SerializeField] private ResourceWallet wallet;
    [SerializeField] private EnemyRegistry registry;
    [SerializeField] private LayerMask placementLayers;
    private TurretDefinition selected;
    private TowerPlacementSlot hoveredSlot;
    public TurretDefinition Selected => selected;
    public event Action<string> Message;
    public event Action<TurretDefinition> Placed;
    public event Action<TurretDefinition> SelectionChanged;
    public void Select(TurretDefinition definition) {
        if (!flow.IsPlaying) return;
        selected = definition; SelectionChanged?.Invoke(selected);
    }
    private void Update() {
        if (!flow.IsPlaying || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) {
            SetHoveredSlot(null); return;
        }
        Vector2 point = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(point, placementLayers);
        TowerPlacementSlot slot = hit != null ? hit.GetComponent<TowerPlacementSlot>() : null;
        SetHoveredSlot(slot);
        if (selected != null && Input.GetMouseButtonDown(0)) TryPlace(slot);
    }
    private void SetHoveredSlot(TowerPlacementSlot slot) {
        if (hoveredSlot != null) hoveredSlot.SetHovered(false);
        hoveredSlot = slot;
        if (hoveredSlot != null) hoveredSlot.SetHovered(true);
    }
    private void OnDisable() { SetHoveredSlot(null); }
    public bool TryPlace(TowerPlacementSlot slot) {
        if (!flow.IsPlaying || selected == null || !GetComponent<GameplayHand>().CanPlace(selected)) return false;
        if (slot == null) return Fail("You cannot place a turret here.");
        if (slot.IsOccupied) return Fail("A turret is already placed here.");
        if (!wallet.CanAfford(selected.Cost)) return Fail("Not enough resources.");
        if (selected.Prefab == null || selected.Targeting == null) {
            Debug.LogError("Selected turret requires a prefab and targeting strategy.", this); return false;
        }
        Turret turret = Instantiate(selected.Prefab, slot.transform.position, Quaternion.identity, slot.transform);
        turret.Initialize(selected, registry);
        if (!slot.TryOccupy(turret)) { Destroy(turret.gameObject); return Fail("A turret is already placed here."); }
        wallet.TrySpend(selected.Cost);
        var used = selected;
        Placed?.Invoke(used);
        Message?.Invoke("Turret placed."); return true;
    }
    private bool Fail(string message) { Message?.Invoke(message); return false; }
}
}
