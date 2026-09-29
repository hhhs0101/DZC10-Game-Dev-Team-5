using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace Defense {
// Small placeholder UI helper; replace visuals without changing gameplay components.
public static class UiFactory {
    private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size) {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }
    public static GameObject Panel(string name, Transform parent, Color color) {
        var rect = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
        rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.gameObject.AddComponent<Image>().color = color;
        return rect.gameObject;
    }
    public static Text Label(Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size, int fontSize = 24) {
        var rect = Rect(text, parent, anchor, position, size);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = Font; label.text = text; label.fontSize = fontSize;
        label.color = Color.white; label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }
    public static Button Button(Transform parent, string text, Vector2 position, UnityAction action, Vector2? anchor = null, Vector2? size = null) {
        var rect = Rect(text, parent, anchor ?? new Vector2(.5f,.5f), position, size ?? new Vector2(280, 52));
        rect.gameObject.AddComponent<Image>().color = new Color(.18f,.26f,.34f);
        var button = rect.gameObject.AddComponent<Button>(); button.onClick.AddListener(action);
        Label(rect, text, new Vector2(.5f,.5f), Vector2.zero, rect.sizeDelta, 22);
        return button;
    }
}
}
