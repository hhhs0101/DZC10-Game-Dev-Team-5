using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
namespace Defense {
// Small placeholder UI helper; replace visuals without changing gameplay components.
public static class UiFactory {
    private static Font cachedFont;
    private static Font cachedBannerFont;
    private static Font Font => cachedFont != null ? cachedFont : (cachedFont = Font.CreateDynamicFontFromOSFont(new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Arial" },24));
    public static Font BannerFont => cachedBannerFont != null ? cachedBannerFont : (cachedBannerFont = Font.CreateDynamicFontFromOSFont(new[] { "Trebuchet MS", "Verdana", "Segoe UI", "Arial" }, 26));
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
    public static void ApplyButtonSkin(UnityEngine.UI.Button button, MainMenuSkin.ButtonSprites sprites) {
        button.image.sprite = sprites.Normal; button.image.color = Color.white; button.image.preserveAspect = true;
        button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
        button.spriteState = new UnityEngine.UI.SpriteState {
            highlightedSprite = sprites.Highlighted, selectedSprite = sprites.Highlighted,
            pressedSprite = sprites.Pressed
        };
        // The sheet contains both the icon and lettering. Retain labels for existing tooling only.
        foreach (var label in button.GetComponentsInChildren<UnityEngine.UI.Text>()) label.enabled = false;
    }
    public static Button Button(Transform parent, string text, Vector2 position, UnityAction action, Vector2? anchor = null, Vector2? size = null) {
        var rect = Rect(text, parent, anchor ?? new Vector2(.5f,.5f), position, size ?? new Vector2(280, 52));
        rect.gameObject.AddComponent<Image>().color = new Color(.18f,.26f,.34f);
        var button = rect.gameObject.AddComponent<Button>();
        button.onClick.AddListener(GameAudio.PlayButtonClick);
        button.onClick.AddListener(action);
        Label(rect, text, new Vector2(.5f,.5f), Vector2.zero, rect.sizeDelta, 22);
        return button;
    }
}
}
