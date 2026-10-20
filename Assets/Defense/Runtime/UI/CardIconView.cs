using UnityEngine;
namespace Defense {
// Presentation only: Tower cards resolve the same Sprite as the placed Tower.
public sealed class CardIconView : MonoBehaviour {
    private UnityEngine.UI.Image image;
    public static CardIconView Create(Transform parent, Vector2 position, Vector2 size) {
        var rect = UiFactory.Rect("Card Artwork",parent,new Vector2(.5f,.5f),position,size);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.preserveAspect = true; image.raycastTarget = false;
        var view = rect.gameObject.AddComponent<CardIconView>(); view.image = image;
        return view;
    }
    public bool Bind(CardDefinition card) {
        image.sprite = card != null ? card.Icon : null;
        bool visible = image.sprite != null;
        gameObject.SetActive(visible); return visible;
    }
}
}
