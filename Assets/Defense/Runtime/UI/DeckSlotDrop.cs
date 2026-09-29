using UnityEngine;
using UnityEngine.EventSystems;
namespace Defense {
public sealed class DeckSlotDrop : MonoBehaviour, IDropHandler {
    private DeckEditorController controller;
    private int slot;
    public void Initialize(DeckEditorController editor, int index) { controller = editor; slot = index; }
    public void OnDrop(PointerEventData data) {
        var source = data.pointerDrag != null ? data.pointerDrag.GetComponent<DeckCardDrag>() : null;
        if (source != null && source.IsDragging) controller.Replace(slot,source.Definition);
    }
}
}
