using UnityEngine;
namespace Defense {
[RequireComponent(typeof(SettingsUI))]
public sealed class MainMenuUI : MonoBehaviour {
    [SerializeField] private Canvas canvas;
    [SerializeField, Tooltip("Shared menu background, logo and Normal / Highlighted / Pressed button sprites.")] private MainMenuSkin skin;
    private GameObject menu, lobby, deckEditor;
    private SettingsUI settings;
    private void Start() {
        Time.timeScale = 1; PlayerSession.Ensure();
        BuildBackground();
        menu = UiFactory.Panel("Start Screen",canvas.transform,new Color(0,0,0,.08f));
        menu.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        lobby = UiFactory.Panel("Lobby",canvas.transform,new Color(0,0,0,.20f));
        lobby.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        deckEditor = UiFactory.Panel("Deck Editor",canvas.transform,new Color(.07f,.1f,.15f));
        BuildStartScreen();
        lobby.AddComponent<LobbyUI>().Initialize(()=>Show(deckEditor),()=>OpenSettings(lobby),()=>Show(menu),skin);
        deckEditor.AddComponent<DeckEditorUI>().Initialize((RectTransform)canvas.transform,()=>Show(lobby),()=>OpenSettings(deckEditor),skin);
        settings = GetComponent<SettingsUI>(); settings.Initialize(canvas.transform);
        if (!PlayerSession.ReturnToLobby && IntroVideoUI.ShouldPlay) {
            menu.SetActive(false); lobby.SetActive(false); deckEditor.SetActive(false);
            IntroVideoUI.Play(canvas.transform,()=>Show(menu));
        }
        else Show(PlayerSession.ReturnToLobby ? lobby : menu);
    }
    private void BuildBackground() {
        // Lobby and settings share this background; the start screen draws its own titled page on top.
        var background = UiFactory.LoadSprite("UI/GameBackground");
        if (background == null) background = skin != null ? skin.Background : null;
        if (background == null) throw new System.InvalidOperationException("Assign the Main Menu Skin background sprite.");
        var rect = UiFactory.Rect("Menu Background",canvas.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        rect.SetAsFirstSibling();
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.sprite = background; image.preserveAspect = true; image.raycastTarget = false;
        var fit = rect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
        fit.aspectRatio = background.rect.width / background.rect.height;
        fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
    }
    private void BuildStartScreen() {
        if (skin == null) throw new System.InvalidOperationException("Assign Main Menu Skin on MainMenuUI.");
        // The start screen art has the title painted in, so it replaces the shared background and logo here only.
        var page = Resources.Load<Sprite>("UI/MainMenuPage");
        if (page != null) {
            var pageRect = UiFactory.Rect("Start Screen Page",menu.transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
            pageRect.SetAsFirstSibling();
            var pageImage = pageRect.gameObject.AddComponent<UnityEngine.UI.Image>();
            pageImage.sprite = page; pageImage.preserveAspect = true; pageImage.raycastTarget = false;
            var fit = pageRect.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
            fit.aspectRatio = page.rect.width / page.rect.height;
            fit.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
            // Anchored to the art so the button stays beside the painted "TO HAVE A SHORT MOUTH" line.
            CulturalInfoBox.Create(pageRect,menu.transform,new Vector2(.71f,.565f),new Vector2(.77f,.645f),
                CulturalFacts.TitleHeading,CulturalFacts.TitleBody);
        } else {
            var logo = UiFactory.Rect("Food Defense Logo",menu.transform,Vector2.zero,Vector2.zero,Vector2.zero);
            Stretch(logo,new Vector2(.1f,.59f),new Vector2(.9f,.97f));
            var image = logo.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = skin.Logo; image.preserveAspect = true; image.raycastTarget = false;
        }
        SkinButton("Start Game",skin.StartGame,.39f,.57f,()=>Show(lobby));
        SkinButton("Settings",skin.Settings,.20f,.38f,()=>OpenSettings(menu));
        SkinButton("Exit",skin.Exit,.01f,.19f,Quit);
    }
    private void SkinButton(string label, MainMenuSkin.ButtonSprites sprites, float bottom, float top, UnityEngine.Events.UnityAction callback) {
        var button = UiFactory.Button(menu.transform,label,Vector2.zero,callback);
        Stretch((RectTransform)button.transform,new Vector2(.30f,bottom),new Vector2(.70f,top));
        button.image.sprite = sprites.Normal;
        button.image.color = Color.white;
        button.image.preserveAspect = true;
        button.transition = UnityEngine.UI.Selectable.Transition.SpriteSwap;
        button.spriteState = new UnityEngine.UI.SpriteState {
            highlightedSprite = sprites.Highlighted, selectedSprite = sprites.Highlighted,
            pressedSprite = sprites.Pressed, disabledSprite = sprites.Pressed
        };
        // Keep the existing label available to UI tooling, but do not draw text twice.
        foreach (var text in button.GetComponentsInChildren<UnityEngine.UI.Text>()) text.enabled = false;
    }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max) {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private void Show(GameObject panel) {
        menu.SetActive(panel==menu); lobby.SetActive(panel==lobby); deckEditor.SetActive(panel==deckEditor);
        if (panel == menu) GameAudio.PlayIntroBgm();
        else GameAudio.PlayHomeBgm();
    }
    private void OpenSettings(GameObject origin) {
        origin.SetActive(false);
        GameAudio.PlayHomeBgm();
        settings.Open(()=>Show(origin));
    }
    private static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
}
