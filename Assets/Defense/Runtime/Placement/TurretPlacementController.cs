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
    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private SkillAreaVisual rangeVisual;
    [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.62f);
    [SerializeField] private Color rangeColor = new Color(0.2f, 0.85f, 1f, 0.75f);

    public TurretDefinition Selected => selected;
    public TowerPlacementSlot HoveredSlot => hoveredSlot;
    public BattlefieldPointer2D Pointer2D => pointer2D;
    public GameObject PreviewObject => previewObject;
    public SpriteRenderer PreviewRenderer => previewRenderer;
    public SkillAreaVisual RangeVisual => rangeVisual;

    public event Action<string> Message;
    public event Action<TurretDefinition> Placed;
    public event Action<TurretDefinition> SelectionChanged;

    public void Select(TurretDefinition definition) {
        if (flow != null && !flow.IsPlaying) return;
        selected = definition;
        if (selected == null) SetHoveredSlot(null);
        else UpdatePreview();
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

    public void SetHoveredSlot(TowerPlacementSlot slot) {
        if (hoveredSlot == slot) return;
        if (hoveredSlot != null) hoveredSlot.SetHovered(false);
        hoveredSlot = slot;
        if (hoveredSlot != null) hoveredSlot.SetHovered(true);
        UpdatePreview();
    }

    private void OnDisable() {
        SetHoveredSlot(null);
        HidePreview();
    }

    private void OnDestroy() {
        if (previewObject != null) {
            if (Application.isPlaying) Destroy(previewObject);
            else DestroyImmediate(previewObject);
        }
        if (rangeVisual != null) {
            if (Application.isPlaying) Destroy(rangeVisual.gameObject);
            else DestroyImmediate(rangeVisual.gameObject);
        }
    }

    public void EnsurePreview() {
        if (previewObject == null) {
            previewObject = new GameObject("Tower Placement Ghost Preview");
            previewObject.transform.SetParent(transform, false);
            previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sortingOrder = 25;
            previewObject.SetActive(false);
        }
        if (rangeVisual == null) {
            var rangeObj = new GameObject("Tower Placement Range Preview");
            rangeObj.transform.SetParent(transform, false);
            rangeVisual = rangeObj.AddComponent<SkillAreaVisual>();
            rangeVisual.Hide();
        }
    }

    public void UpdatePreview() {
        EnsurePreview();
        if (selected == null || hoveredSlot == null || hoveredSlot.IsOccupied || (flow != null && !flow.IsPlaying)) {
            HidePreview();
            return;
        }

        var sprite = ResolveTowerSprite(selected);
        if (sprite != null) {
            previewRenderer.sprite = sprite;
            float scale = selected.SpriteScale > 0.01f ? selected.SpriteScale : 1f;
            previewObject.transform.localScale = new Vector3(scale, scale, 1f);
            previewObject.transform.position = hoveredSlot.PlacementPoint.position + new Vector3(selected.SpriteOffset.x, selected.SpriteOffset.y, 0f);
            previewRenderer.color = previewColor;
            previewRenderer.sortingOrder = 25;
            previewObject.SetActive(true);
        } else {
            previewObject.SetActive(false);
        }

        if (selected.AttackRange > 0.01f) {
            rangeVisual.Show(hoveredSlot.PlacementPoint.position, selected.AttackRange, rangeColor, WaypointPlane.XY);
        } else {
            rangeVisual.Hide();
        }
    }

    public void HidePreview() {
        if (previewObject != null) previewObject.SetActive(false);
        if (rangeVisual != null) rangeVisual.Hide();
    }

    private Sprite ResolveTowerSprite(TurretDefinition def) {
        if (def == null) return null;
        if (def.Sprite != null) return def.Sprite;
        if (def.Prefab != null) {
            var sr = def.Prefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null) return sr.sprite;
        }
        if (spritePrefab != null) {
            var sr = spritePrefab.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null) return sr.sprite;
        }
        return null;
    }

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
        SetHoveredSlot(null);
        HidePreview();
        hand.CompleteUse(card, CardUseResult.Success);
        GameAudio.PlayTowerPlaced(used);
        Placed?.Invoke(used);
        Message?.Invoke("Tower placed."); return true;
    }
    private bool Fail(string message) { Message?.Invoke(message); return false; }
}
}
