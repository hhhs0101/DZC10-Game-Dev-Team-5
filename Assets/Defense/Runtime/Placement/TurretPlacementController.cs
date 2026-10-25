using System;
using UnityEngine;
namespace Defense {
[RequireComponent(typeof(GameplayHand))]
public sealed class TurretPlacementController : MonoBehaviour {
    [SerializeField] private Camera worldCamera;
    [SerializeField] private GameFlow flow;
    [SerializeField] private ResourceWallet wallet;
    [SerializeField] private EnemyRegistry registry;
    [SerializeField, Tooltip("Required 2D map and slot input.")] private BattlefieldPointer2D pointer2D;
    [SerializeField, Tooltip("Optional 2D actor prefab; selected definition still supplies all combat/cost data.")] private Tower spritePrefab;
    [SerializeField, Tooltip("Optional read-only placed-Tower inspection for native 2D scenes.")] private PlacedTowerInspectionController inspection;
    private TurretDefinition selected;
    private TowerPlacementSlot hoveredSlot;
    public TurretDefinition Selected => selected;
    public event Action<string> Message;
    public event Action<TurretDefinition> Placed;
    public event Action<TurretDefinition> SelectionChanged;
    public void Select(TurretDefinition definition) {
        if (!flow.IsPlaying) return;
        selected = definition;
        if (selected == null) SetHoveredSlot(null);
        SelectionChanged?.Invoke(selected);
    }
    private void Update() => ProcessPointer(Input.mousePosition,Input.GetMouseButtonDown(0));
    // The live mouse and integration tests share the same screen-coordinate input path.
    public bool ProcessPointer(Vector2 screenPosition, bool pressed) {
        if (inspection != null) inspection.ProcessPointer(screenPosition,pressed);
        if (!flow.IsPlaying || selected == null || worldCamera == null ||
            !worldCamera.pixelRect.Contains(screenPosition) || GameplayPointer.IsOverUI(screenPosition)) {
            SetHoveredSlot(null); return false;
        }
        TowerPlacementSlot slot = pointer2D != null && pointer2D.TryGetPoint(screenPosition,out var world) ? pointer2D.GetSlot(world) : null;
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
        if (slot == null || (pointer2D != null && !pointer2D.Map.PlayableArea.Contains(slot.PlacementPoint.position))) return Fail("You cannot place a turret here.");
        if (slot.IsOccupied) return Fail("A turret is already placed here.");
        if (selected.Prefab == null || selected.Targeting == null) {
            Debug.LogError("Selected turret requires a prefab and targeting strategy.", this); return false;
        }
        var hand = GetComponent<GameplayHand>();
        var card = hand.SelectedCard;
        if (!hand.CanUse(card)) return false;
        Tower turret = Instantiate(selected.Prefab, slot.PlacementPoint.position, slot.PlacementPoint.rotation, slot.transform);
        turret.Initialize(selected, registry);
        if (!slot.TryOccupy(turret)) { Destroy(turret.gameObject); return Fail("A turret is already placed here."); }
        turret.StartLifetime(slot, flow, card.Definition.ElixirCost);
        var used = selected;
        hand.CompleteUse(card, CardUseResult.Success);
        GameAudio.PlayTowerPlaced(used);
        Placed?.Invoke(used);
        Message?.Invoke("Tower placed."); return true;
    }
    private bool Fail(string message) { Message?.Invoke(message); return false; }
}
}
