using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Shared by battle, deck editor, lobby and drag previews. Reads the actual gameplay cost.
public sealed class CardEnergyBadge : MonoBehaviour {
    private static Sprite energySprite;
    private Text number;
    public static CardEnergyBadge Create(RectTransform card) {
        if (energySprite == null) energySprite = Resources.Load<Sprite>("UI/EnergyIcon");
        float width = card.sizeDelta.x * .38f;
        float height = width * 939f / 612f;
        // Keep the top inside the card so scroll view masks do not clip the bolt.
        var rect = UiFactory.Rect("Card Energy",card,new Vector2(0,1),
            new Vector2(width*.35f,-height*.5f),new Vector2(width,height));
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = energySprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = energySprite != null ? Color.white : new Color(1f,.84f,.12f);
        var badge = rect.gameObject.AddComponent<CardEnergyBadge>();
        // Center the number in the wide upper section of the lightning bolt.
        badge.number = UiFactory.Label(rect,"",new Vector2(.43f,.73f),Vector2.zero,
            new Vector2(width*1.05f,height*.54f),Mathf.RoundToInt(width*.80f));
        badge.number.name = "Energy Number";
        badge.number.color = Color.white;
        badge.number.fontStyle = FontStyle.Normal;
        badge.number.resizeTextForBestFit = true;
        badge.number.resizeTextMinSize = 14;
        badge.number.resizeTextMaxSize = Mathf.RoundToInt(width*.80f);
        badge.number.horizontalOverflow = HorizontalWrapMode.Overflow;
        badge.number.verticalOverflow = VerticalWrapMode.Overflow;
        var outline = badge.number.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(0.6f,-0.6f);
        outline.useGraphicAlpha = true;
        return badge;
    }
    public void Bind(CardDefinition card) {
        gameObject.SetActive(card != null);
        if (card == null) return;
        number.text = card.ElixirCost.ToString(CultureInfo.InvariantCulture);
    }
}
}
