using System;
using UnityEngine;
namespace Defense {
[RequireComponent(typeof(GameplayHand))]
public sealed class TurretPlacementController : MonoBehaviour {
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameFlow flow;
    [SerializeField] private ResourceWallet wallet;
    [SerializeField] private EnemyRegistry registry;
    [SerializeField] private LayerMask placementLayers;
    [SerializeField] private TabletopSurface surface;
    [SerializeField, Tooltip("Native 2D input; leave empty for legacy 3D scenes.")] private BattlefieldPointer2D pointer2D;
    [SerializeField, Tooltip("Optional 2D actor prefab; selected definition still supplies all combat/cost data.")] private Turret spritePrefab;
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
    private void Update() => ProcessPointer(Input.mousePosition,Input.GetMouseButtonDown(0));
    // The live mouse and integration tests share the same screen-coordinate input path.
    public bool ProcessPointer(Vector2 screenPosition, bool pressed) {
        if (!flow.IsPlaying || selected == null || worldCamera == null ||
            !worldCamera.pixelRect.Contains(screenPosition) || GameplayPointer.IsOverUI(screenPosition)) {
            SetHoveredSlot(null); return false;
        }
        if (pointer2D != null) {
            TowerPlacementSlot slot2D = pointer2D.TryGetPoint(screenPosition,out var world) ? pointer2D.GetSlot(world) : null;
            SetHoveredSlot(slot2D != null && !slot2D.IsOccupied ? slot2D : null);
            return pressed && TryPlace(slot2D);
        }
        var ray = worldCamera.ScreenPointToRay(screenPosition);
        TowerPlacementSlot slot = null;
        // Only slot colliders participate: table/enemy colliders cannot mask a valid slot.
        if (Physics.Raycast(ray,out var hit,worldCamera.farClipPlane,placementLayers,QueryTriggerInteraction.Ignore))
            slot = hit.collider.GetComponentInParent<TowerPlacementSlot>();
        SetHoveredSlot(slot != null && !slot.IsOccupied ? slot : null);
        return pressed && TryPlace(slot);
    }
    private void SetHoveredSlot(TowerPlacementSlot slot) {
        if (hoveredSlot != null) hoveredSlot.SetHovered(false);
        hoveredSlot = slot;
        if (hoveredSlot != null) hoveredSlot.SetHovered(true);
    }
    private void OnDisable() { SetHoveredSlot(null); }
    public bool TryPlace(TowerPlacementSlot slot) {
        if (!flow.IsPlaying || selected == null || !GetComponent<GameplayHand>().CanPlace(selected)) return false;
        if (slot == null || (pointer2D != null && !pointer2D.Map.PlayableArea.Contains(slot.PlacementPoint.position)) || (surface != null && !surface.Contains(PlanarSpace.Project(slot.PlacementPoint.position)))) return Fail("You cannot place a turret here.");
        if (slot.IsOccupied) return Fail("A turret is already placed here.");
        if (!wallet.CanAfford(selected.Cost)) return Fail("Not enough resources.");
        if (selected.Prefab == null || selected.Targeting == null) {
            Debug.LogError("Selected turret requires a prefab and targeting strategy.", this); return false;
        }
        var hand = GetComponent<GameplayHand>();
        var card = hand.SelectedCard;
        if (!hand.CanUse(card)) return false;
        Turret turret = Instantiate(spritePrefab != null ? spritePrefab : selected.Prefab, slot.PlacementPoint.position, slot.PlacementPoint.rotation, slot.transform);
        turret.Initialize(selected, registry);
        if (!slot.TryOccupy(turret)) { Destroy(turret.gameObject); return Fail("A turret is already placed here."); }
        wallet.TrySpend(selected.Cost);
        var used = selected;
        hand.CompleteUse(card, CardUseResult.Success);
        Placed?.Invoke(used);
        Message?.Invoke("Turret placed."); return true;
    }
    private bool Fail(string message) { Message?.Invoke(message); return false; }
}
}
