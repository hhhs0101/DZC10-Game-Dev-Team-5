using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Defense {
// Drag-and-drop handler for tower cards in the gameplay battle hand.
public sealed class HandCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private int cardIndex;
    private GameplayHand hand;
    private TurretPlacementController placement;
    private RectTransform canvasRoot;
    private RectTransform ghost;
    private CanvasGroup buttonGroup;

    public bool IsDragging { get; private set; }
    public bool WasDragging { get; private set; }

    public void Initialize(int index, GameplayHand gameplayHand, TurretPlacementController placementController, RectTransform canvas) {
        cardIndex = index;
        hand = gameplayHand;
        placement = placementController;
        canvasRoot = canvas;
        buttonGroup = GetComponent<CanvasGroup>();
        if (buttonGroup == null) buttonGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void LateUpdate() {
        if (!IsDragging) {
            WasDragging = false;
        }
    }

    private void Update() {
        if (IsDragging && Input.GetMouseButtonDown(1)) {
            Cancel();
            if (hand != null) hand.CancelSelection();
        }
    }

    public void OnBeginDrag(PointerEventData data) {
        if (!isActiveAndEnabled || IsDragging || data.button != PointerEventData.InputButton.Left) return;
        if (hand == null || hand.Cycle == null || cardIndex < 0 || cardIndex >= hand.Cycle.Hand.Count) return;
        var runtimeCard = hand.Cycle.Hand[cardIndex];
        if (runtimeCard == null || !(runtimeCard.Definition is TowerCardDefinition towerDef)) return;

        IsDragging = true;
        WasDragging = true;
        hand.Select(cardIndex);

        if (buttonGroup != null) buttonGroup.alpha = 0.45f;

        CreateGhost(towerDef);
        OnDrag(data);
    }

    private void CreateGhost(TowerCardDefinition towerDef) {
        if (canvasRoot == null) return;
        ghost = UiFactory.Rect("Dragged Hand Card Ghost", canvasRoot, new Vector2(.5f, .5f), Vector2.zero, new Vector2(84f, 132f));
        var cg = ghost.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        var ghostImage = ghost.gameObject.AddComponent<Image>();
        ghostImage.preserveAspect = true;
        ghostImage.raycastTarget = false;
        bool hasSprite = towerDef.CardSprite != null;
        ghostImage.sprite = hasSprite ? towerDef.CardSprite : null;
        ghostImage.color = hasSprite ? new Color(1f, 1f, 1f, 0.9f) : new Color(towerDef.Color.r, towerDef.Color.g, towerDef.Color.b, 0.9f);

        if (!hasSprite) {
            CardIconView.Create(ghost, new Vector2(0f, 8f), new Vector2(45f, 64f)).Bind(towerDef);
        }

        var ghostLabel = UiFactory.Label(ghost, towerDef.DisplayName, new Vector2(.5f, .5f), new Vector2(0f, -44f), new Vector2(80f, 32f), 12);
        ghostLabel.raycastTarget = false;
        var ghostOutline = ghostLabel.gameObject.AddComponent<Outline>();
        ghostOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        ghostOutline.effectDistance = new Vector2(1.2f, -1.2f);
        CardTextBackdrop.Create(ghost, ghostLabel);
        CardEnergyBadge.Create(ghost).Bind(towerDef);

        foreach (var g in ghost.GetComponentsInChildren<Graphic>(true)) {
            g.raycastTarget = false;
        }
    }

    public void OnDrag(PointerEventData data) {
        if (!IsDragging) return;
        if (ghost != null && canvasRoot != null) {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, data.position, data.pressEventCamera, out var point)) {
                ghost.anchoredPosition = point;
            }
        }
        if (placement != null) {
            placement.ProcessPointer(data.position, false);
        }
    }

    public void OnEndDrag(PointerEventData data) {
        if (!IsDragging) return;
        IsDragging = false;
        WasDragging = true;
        if (buttonGroup != null) buttonGroup.alpha = 1f;
        if (ghost != null) {
            Destroy(ghost.gameObject);
            ghost = null;
        }

        if (placement != null) {
            var targetSlot = placement.HoveredSlot;
            if (targetSlot == null && placement.Pointer2D != null && placement.Pointer2D.TryGetPoint(data.position, out var world)) {
                var candidate = placement.Pointer2D.GetSlot(world);
                if (candidate != null && !candidate.IsOccupied) targetSlot = candidate;
            }

            if (targetSlot != null && !targetSlot.IsOccupied) {
                bool placed = placement.TryPlace(targetSlot);
                if (!placed && hand != null) {
                    hand.CancelSelection();
                }
            } else {
                if (hand != null) hand.CancelSelection();
            }
        }
    }

    private void OnDisable() {
        Cancel();
    }

    public void Cancel() {
        IsDragging = false;
        WasDragging = false;
        if (buttonGroup != null) buttonGroup.alpha = 1f;
        if (ghost != null) {
            Destroy(ghost.gameObject);
            ghost = null;
        }
    }
}
}

