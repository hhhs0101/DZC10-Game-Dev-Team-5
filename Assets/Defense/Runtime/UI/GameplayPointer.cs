using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Defense {
public static class GameplayPointer {
    // Query the current position, not EventSystem's potentially previous-frame hover cache.
    public static bool IsOverUI(Vector2 screenPosition) {
        if (EventSystem.current == null) return false;
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition },results);
        foreach (var result in results) if (result.module is GraphicRaycaster) return true;
        return false;
    }
}
}
