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
        ghost = UiFactory.Rect("Dragged Tower",canvas,new Vector2(.5f,.5f),Vector2.zero,new Vector2(100,156));
        var image = ghost.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.sprite = Definition.CardSprite;
        image.color = image.sprite != null ? new Color(1f,1f,1f,.88f) : new Color(.3f,.55f,.7f,.85f);
        image.raycastTarget = false;
        var label = UiFactory.Label(ghost,Definition.DisplayName,new Vector2(.5f,.5f),new Vector2(0,-50),new Vector2(96,40),13);
        var outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f,0f,0f,.95f);
        outline.effectDistance = new Vector2(1.2f,-1.2f);
        OnDrag(data);
    }
    public void OnDrag(PointerEventData data) {
        if (!IsDragging) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,data.position,data.pressEventCamera,out var point)) ghost.anchoredPosition = point;
    }
    public void OnEndDrag(PointerEventData data) { Cancel(); }
    private void OnDisable() { Cancel(); }
    public void Cancel() { IsDragging = false; if (ghost != null) { ghost.gameObject.SetActive(false); Destroy(ghost.gameObject); ghost = null; } }
}
}
