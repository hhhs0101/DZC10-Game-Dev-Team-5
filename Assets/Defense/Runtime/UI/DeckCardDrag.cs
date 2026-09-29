using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Defense {
public sealed class DeckCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    public CardDefinition Definition { get; private set; }
    public bool IsDragging { get; private set; }
    private RectTransform ghost;
    private RectTransform canvas;
    public void Initialize(CardDefinition definition, RectTransform canvasRoot) { Definition = definition; canvas = canvasRoot; }
    public void OnBeginDrag(PointerEventData data) {
        if (!isActiveAndEnabled || IsDragging || data.button != PointerEventData.InputButton.Left || Definition == null) return;
        IsDragging = true;
        ghost = UiFactory.Rect("Dragged Tower",canvas,new Vector2(.5f,.5f),Vector2.zero,new Vector2(160,100));
        var image = ghost.gameObject.AddComponent<Image>(); image.color = new Color(.3f,.55f,.7f,.85f); image.raycastTarget = false;
        UiFactory.Label(ghost,Definition.DisplayName,new Vector2(.5f,.5f),Vector2.zero,new Vector2(155,95),20);
        OnDrag(data);
    }
    public void OnDrag(PointerEventData data) {
        if (!IsDragging) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,data.position,data.pressEventCamera,out var point)) ghost.anchoredPosition = point;
    }
    public void OnEndDrag(PointerEventData data) { Cancel(); }
    private void OnDisable() { Cancel(); }
    private void Cancel() { IsDragging = false; if (ghost != null) Destroy(ghost.gameObject); }
}
}
