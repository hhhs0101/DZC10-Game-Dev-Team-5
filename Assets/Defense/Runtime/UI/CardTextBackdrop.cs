using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// A translucent footer above the artwork and behind the card caption.
public static class CardTextBackdrop {
    public static void Create(RectTransform card, Text caption) {
        var rect = UiFactory.Rect("Card Text Backdrop",card,new Vector2(.5f,0),
            new Vector2(0,3),new Vector2(card.sizeDelta.x-6,card.sizeDelta.y*.20f));
        rect.pivot = new Vector2(.5f,0);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0f,0f,0f,.65f);
        image.raycastTarget = false;
        // Keep captions centered in the shorter bottom strip on every card size.
        var textRect = caption.rectTransform;
        textRect.anchorMin = textRect.anchorMax = new Vector2(.5f,0);
        textRect.anchoredPosition = new Vector2(0,3+card.sizeDelta.y*.10f);
        textRect.sizeDelta = new Vector2(card.sizeDelta.x-10,card.sizeDelta.y*.20f-4);
        caption.resizeTextForBestFit = true;
        caption.resizeTextMinSize = 8;
        caption.resizeTextMaxSize = 12;
        caption.transform.SetAsLastSibling();
    }
}
}
