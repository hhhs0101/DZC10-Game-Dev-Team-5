using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Text for the "i" info buttons that explain Korean food culture around the game.
public static class CulturalFacts {
    public const string TitleHeading = "입이 짧다 - To Have a Short Mouth";
    public const string TitleBody =
        "입이 짧다 (ib-i jjal-da) literally means \"the mouth is short.\" In Korean it describes someone who is a picky eater, "
        + "or who only eats a few bites before saying they are full.\n\n"
        + "It is a phrase almost every Korean child hears from their parents or grandparents. When a kid pushes away the kimchi, "
        + "leaves the vegetables in their bowl, or only eats the rice, a parent will sigh: \"넌 왜 이렇게 입이 짧니?\" "
        + "(\"Why is your mouth so short?\")\n\n"
        + "Korean meals are shared, with rice, soup and many small side dishes (반찬, banchan) on the table for everyone. "
        + "Trying a bit of everything and finishing your bowl is a way of showing respect for the food and the person who cooked it.";

    // Boss cards shown just before each stage's final vegetable appears, keyed by enemy ID.
    public static bool TryGetBoss(string enemyTypeId, out string heading, out string body) {
        switch (enemyTypeId) {
            case "enemy_baechu":
                heading = "배추 - Napa Cabbage";
                body = "배추 (baechu) is napa cabbage, the main ingredient of Korea's most famous dish: baechu kimchi.\n\n"
                    + "Every late autumn, families and neighbours gather for 김장 (gimjang) to salt, season and pack hundreds of cabbages "
                    + "so there is kimchi all winter. Kimjang was added to UNESCO's Intangible Cultural Heritage list in 2013.\n\n"
                    + "To a picky eater, though, a whole cabbage is a mountain of leaves!";
                return true;
            case "enemy_gosari":
                heading = "고사리 - Gosari (Bracken Fern)";
                body = "고사리 (gosari) are young bracken fern shoots, picked in the mountains in spring, then dried and soaked before cooking.\n\n"
                    + "They are a classic bibimbap topping and give yukgaejang, a spicy beef soup, its chewy bite. Brown gosari is also one of "
                    + "the three-colour namul (삼색나물) served at ancestral rites and holidays like Chuseok.\n\n"
                    + "Kids often pick them out because they look like little brown worms!";
                return true;
            case "enemy_doraji":
                heading = "도라지 - Doraji (Bellflower Root)";
                body = "도라지 (doraji) is the root of the balloon flower. It is famously bitter, so cooks soak it in salt water "
                    + "before making doraji namul or sweet-and-spicy doraji muchim.\n\n"
                    + "It is also a traditional remedy: doraji tea and syrup are given for coughs and sore throats. White doraji is "
                    + "another colour of 삼색나물, and \"Doraji Taryeong\" is one of Korea's best-known folk songs.\n\n"
                    + "The bitterest boss of all, so season it well!";
                return true;
        }
        heading = body = null;
        return false;
    }
}

// An "i" button that opens a wooden text box with a heading and body text. Click anywhere to close.
public sealed class CulturalInfoBox : MonoBehaviour {
    private GameObject popup;

    public static CulturalInfoBox Create(Transform parent, Transform popupParent, Vector2 anchorMin, Vector2 anchorMax, string heading, string body) {
        var rect = UiFactory.Rect("Info Button",parent,Vector2.zero,Vector2.zero,Vector2.zero);
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = UiFactory.LoadSprite("UI/Ibox");
        image.preserveAspect = true;
        if (image.sprite == null) image.color = new Color(.85f,.6f,.35f);
        var button = rect.gameObject.AddComponent<Button>();
        var hovered = UiFactory.LoadSprite("UI/Ibox_hovered");
        if (hovered != null) {
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { highlightedSprite = hovered, selectedSprite = hovered, pressedSprite = hovered };
        }
        var info = rect.gameObject.AddComponent<CulturalInfoBox>();
        info.popup = BuildPopup(popupParent,heading,body);
        button.onClick.AddListener(GameAudio.PlayButtonClick);
        button.onClick.AddListener(info.Open);
        return info;
    }

    public void Open() { popup.SetActive(true); popup.transform.SetAsLastSibling(); }
    public void Close() { popup.SetActive(false); }

    private void OnDisable() { if (popup != null) popup.SetActive(false); }
    private void OnDestroy() { if (popup != null) Destroy(popup); }

    private static GameObject BuildPopup(Transform parent, string heading, string body) {
        var overlay = UiFactory.Panel("Info Popup",parent,new Color(0,0,0,.55f));
        var dismiss = overlay.AddComponent<Button>();
        dismiss.transition = Selectable.Transition.None;
        dismiss.onClick.AddListener(() => overlay.SetActive(false));

        var box = UiFactory.Rect("Text Info Box",overlay.transform,Vector2.zero,Vector2.zero,Vector2.zero);
        Stretch(box,new Vector2(.18f,.08f),new Vector2(.82f,.92f));
        var boxImage = box.gameObject.AddComponent<Image>();
        boxImage.sprite = UiFactory.LoadSprite("UI/Text Info Box");
        boxImage.preserveAspect = true;
        if (boxImage.sprite == null) boxImage.color = new Color(.86f,.65f,.38f);
        // Fit the frame to the sprite so the text area below lines up with the painted parchment.
        var fit = box.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = boxImage.sprite != null ? boxImage.sprite.rect.width / boxImage.sprite.rect.height : 4f / 3f;

        var ink = new Color(.24f,.12f,.05f);
        var title = UiFactory.Label(box,heading,Vector2.zero,Vector2.zero,Vector2.zero,34);
        Stretch(title.rectTransform,new Vector2(.12f,.78f),new Vector2(.88f,.88f));
        title.color = ink; title.fontStyle = FontStyle.Bold;
        title.resizeTextForBestFit = true; title.resizeTextMinSize = 14; title.resizeTextMaxSize = 34;

        var text = UiFactory.Label(box,body,Vector2.zero,Vector2.zero,Vector2.zero,22);
        Stretch(text.rectTransform,new Vector2(.12f,.18f),new Vector2(.88f,.76f));
        text.color = ink; text.alignment = TextAnchor.UpperLeft; text.lineSpacing = 1.1f;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = 24;

        var hint = UiFactory.Label(box,"Click anywhere to close",Vector2.zero,Vector2.zero,Vector2.zero,16);
        Stretch(hint.rectTransform,new Vector2(.12f,.1f),new Vector2(.88f,.16f));
        hint.color = new Color(ink.r,ink.g,ink.b,.65f); hint.fontStyle = FontStyle.Italic;
        hint.resizeTextForBestFit = true; hint.resizeTextMinSize = 10; hint.resizeTextMaxSize = 16;

        overlay.SetActive(false);
        return overlay;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max) {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
}
