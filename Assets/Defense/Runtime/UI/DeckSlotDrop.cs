using System;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Defense {
public sealed class DeckSlotDrop : MonoBehaviour, IDropHandler, IPointerClickHandler {
    private DeckEditorController controller;
    private int slot;
    private Action<int> onClick;
    public void Initialize(DeckEditorController editor, int index, Action<int> clickCallback = null) {
        controller = editor;
        slot = index;
        if (clickCallback != null) onClick = clickCallback;
    }
    public void OnDrop(PointerEventData data) {
        var source = data.pointerDrag != null ? data.pointerDrag.GetComponent<DeckCardDrag>() : null;
        if (source != null && source.IsDragging) controller.Replace(slot,source.Definition);
    }
    public void OnPointerClick(PointerEventData data) {
        if (!isActiveAndEnabled || data.dragging || data.button != PointerEventData.InputButton.Left) return;
        GameAudio.PlayButtonClick();
        onClick?.Invoke(slot);
    }
}
}
