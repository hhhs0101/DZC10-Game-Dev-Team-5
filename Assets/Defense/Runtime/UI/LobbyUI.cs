using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Defense {
public sealed class LobbyUI : MonoBehaviour {
    [SerializeField, Min(.01f)] private float initialHoldDelay = .4f;
    [SerializeField, Min(.01f)] private float repeatInterval = .15f;
    private readonly KeyRepeat repeat = new KeyRepeat();
    private Text feedback;
    private RectTransform unlockPopupOverlay;
    private readonly Image[] unlockCardImages = new Image[3];
    private readonly CardIconView[] unlockCardIcons = new CardIconView[3];
    private readonly Text[] unlockCardLabels = new Text[3];
    private readonly CardEnergyBadge[] unlockCardBadges = new CardEnergyBadge[3];
    public StageCarouselView Carousel { get; private set; }
    public void Initialize(Action openDeck, Action settings, Action back, MainMenuSkin skin = null) {
        UiFactory.Label(transform,"Lobby",new Vector2(.5f,1),new Vector2(0,-45),new Vector2(350,60),36);
        var backButton = UiFactory.Button(transform,"Back",new Vector2(95,-40),()=>back(),new Vector2(0,1),new Vector2(160,48));
        var settingsButton = UiFactory.Button(transform,"Settings",new Vector2(-100,-40),()=>settings(),new Vector2(1,1),new Vector2(180,48));
        if (skin != null) {
            UiFactory.ApplyButtonSkin(backButton,skin.LobbyBack);
            UiFactory.ApplyButtonSkin(settingsButton,skin.LobbySettings);
        }
        var viewport = UiFactory.Rect("Stage Carousel",transform,new Vector2(.5f,1),new Vector2(0,-200),new Vector2(970,270));
        Carousel = viewport.gameObject.AddComponent<StageCarouselView>();
        Carousel.Initialize(PlayerSession.Catalog,PlayerSession.Selection,PlayerSession.Progression,skin);
        var preview = UiFactory.Rect("Stage enemy preview",transform,new Vector2(.5f,1),new Vector2(0,-402),new Vector2(950,120));
        preview.gameObject.AddComponent<StageEnemyPreview>().Initialize(PlayerSession.Catalog,PlayerSession.Selection);
        feedback = UiFactory.Label(transform,"A / D or click side cards   |   Enter: play",new Vector2(.5f,1),new Vector2(0,-485),new Vector2(850,32),19);
        var enterStageButton = UiFactory.Button(transform,"Enter Stage",new Vector2(0,-535),()=>TryEnter(),new Vector2(.5f,1),new Vector2(324,60));
        // Keep the preview and all three actions visible on the 1280 x 720 menu.
        var guideButton = UiFactory.Button(transform,"How to Play",new Vector2(0,-605),()=>TutorialUI.Open(GetComponentInParent<Canvas>().transform),new Vector2(.5f,1),new Vector2(324,60));
        var guideSprite = UiFactory.LoadSprite("UI/HowToPlayButton");
        if(guideSprite != null) {
            guideButton.image.sprite = guideSprite;
            guideButton.image.color = Color.white;
            guideButton.image.preserveAspect = true;
            guideButton.transition = Selectable.Transition.ColorTint;
            var colors = guideButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f,1f,.86f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(.78f,.67f,.54f);
            colors.fadeDuration = .08f;
            guideButton.colors = colors;
            foreach(var label in guideButton.GetComponentsInChildren<Text>()) label.enabled = false;
        }
        var deckButton = UiFactory.Button(transform,"Deck",new Vector2(0,45),()=>openDeck(),new Vector2(.5f,0),new Vector2(324,60));
        if (skin != null) {
            UiFactory.ApplyButtonSkin(enterStageButton,skin.LobbyEnterStage);
            UiFactory.ApplyButtonSkin(deckButton,skin.LobbyDeck);
        }
        BuildUnlockPopup();
        CheckUnlockPopup();
    }
    private void OnEnable() {
        CheckUnlockPopup();
    }
    private void BuildUnlockPopup() {
        unlockPopupOverlay = UiFactory.Rect("Unlock Towers Popup Overlay",transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        unlockPopupOverlay.anchorMin = Vector2.zero;
        unlockPopupOverlay.anchorMax = Vector2.one;
        unlockPopupOverlay.offsetMin = unlockPopupOverlay.offsetMax = Vector2.zero;
        var dimmerImage = unlockPopupOverlay.gameObject.AddComponent<Image>();
        dimmerImage.color = new Color(0f,0f,0f,.68f);
        var dimmerButton = unlockPopupOverlay.gameObject.AddComponent<Button>();
        dimmerButton.transition = Selectable.Transition.None;
        dimmerButton.onClick.AddListener(GameAudio.PlayButtonClick);
        dimmerButton.onClick.AddListener(HideUnlockPopup);

        var dialog = UiFactory.Rect("Unlock Towers Dialog",unlockPopupOverlay,new Vector2(.5f,.5f),Vector2.zero,new Vector2(680,440));
        var dialogBg = dialog.gameObject.AddComponent<Image>();
        var woodSprite = Resources.Load<Sprite>("UI/TowerCardsWoodPanel");
        if (woodSprite != null) {
            dialogBg.sprite = woodSprite;
            dialogBg.color = Color.white;
        } else {
            dialogBg.color = new Color(.22f,.15f,.1f,.98f);
        }
        dialogBg.raycastTarget = true;

        var innerPanel = UiFactory.Rect("Inner Content Backdrop",dialog,new Vector2(.5f,.5f),new Vector2(0,16),new Vector2(620,336));
        var innerImage = innerPanel.gameObject.AddComponent<Image>();
        innerImage.color = new Color(.12f,.08f,.05f,.82f);
        innerImage.raycastTarget = false;

        var titleText = UiFactory.Label(dialog,"New towers unlocked!",new Vector2(.5f,1f),new Vector2(0,-58),new Vector2(580,48),34);
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f,.92f,.65f);
        var titleOutline = titleText.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f,0f,0f,.95f);
        titleOutline.effectDistance = new Vector2(1.5f,-1.5f);

        for (int i = 0; i < 3; i++) {
            var cardRect = UiFactory.Rect("Unlocked Card Slot "+(i+1),dialog,new Vector2(.5f,.5f),new Vector2((i-1)*168f,18f),new Vector2(124,192));
            unlockCardImages[i] = cardRect.gameObject.AddComponent<Image>();
            unlockCardImages[i].preserveAspect = true;
            unlockCardImages[i].raycastTarget = false;
            unlockCardIcons[i] = CardIconView.Create(cardRect,new Vector2(0,14),new Vector2(62,96));
            unlockCardLabels[i] = UiFactory.Label(cardRect,"",new Vector2(.5f,.5f),new Vector2(0,-58),new Vector2(114,52),14);
            var outline = unlockCardLabels[i].gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f,0f,0f,.95f);
            outline.effectDistance = new Vector2(1.2f,-1.2f);
            CardTextBackdrop.Create(cardRect,unlockCardLabels[i]);
            unlockCardBadges[i] = CardEnergyBadge.Create(cardRect);
        }

        var subText = UiFactory.Label(dialog,"Check deck page",new Vector2(.5f,0f),new Vector2(0,102),new Vector2(500,36),22);
        subText.color = new Color(.92f,.95f,1f);
        var subOutline = subText.gameObject.AddComponent<Outline>();
        subOutline.effectColor = new Color(0f,0f,0f,.95f);
        subOutline.effectDistance = new Vector2(1.2f,-1.2f);

        var closeBtn = UiFactory.Button(dialog,"Close",new Vector2(0,38),HideUnlockPopup,new Vector2(.5f,0f),new Vector2(176,54));
        if (!UiFactory.ApplyCloseButtonSkin(closeBtn))
            closeBtn.GetComponent<Image>().color = new Color(.55f,.24f,.14f,.95f);

        unlockPopupOverlay.gameObject.SetActive(false);
    }
    private static CardDefinition FindCatalogCard(string cardId, string displayName) {
        if (PlayerSession.Catalog == null || PlayerSession.Catalog.AvailableCards == null) return null;
        var list = PlayerSession.Catalog.AvailableCards;
        for (int i = 0; i < list.Count; i++) {
            var c = list[i];
            if (c != null && (c.CardId == cardId || string.Equals(c.DisplayName,displayName,StringComparison.OrdinalIgnoreCase)))
                return c;
        }
        return null;
    }
    private void CheckUnlockPopup() {
        int clearedStage = PlayerSession.PendingTowerUnlockStage;
        if (unlockPopupOverlay == null || (clearedStage != 1 && clearedStage != 2)) return;
        PlayerSession.PendingTowerUnlockStage = 0;

        string[] targetNames = clearedStage == 1
            ? new[] { "Ketchup", "Mustard", "Olive Oil" }
            : new[] { "Hot Sauce", "Soy Sauce", "Mayonnaise" };
        string[] targetIds = clearedStage == 1
            ? new[] { "tower.test.11", "tower.test.9", "tower.test.2" }
            : new[] { "tower.test.3", "tower.test.8", "tower.test.10" };

        for (int i = 0; i < 3; i++) {
            var card = FindCatalogCard(targetIds[i],targetNames[i]);
            if (card == null) {
                unlockCardImages[i].gameObject.SetActive(false);
                continue;
            }
            unlockCardImages[i].gameObject.SetActive(true);
            bool hasCardSprite = card.CardSprite != null;
            unlockCardImages[i].sprite = hasCardSprite ? card.CardSprite : null;
            unlockCardImages[i].color = hasCardSprite ? Color.white : new Color(.2f,.32f,.4f);
            if (hasCardSprite) {
                unlockCardIcons[i].gameObject.SetActive(false);
            } else {
                unlockCardIcons[i].Bind(card);
            }
            unlockCardLabels[i].text = card.DisplayName;
            unlockCardBadges[i].Bind(card);
        }

        if (PlayerSession.Selection != null && PlayerSession.Catalog != null && clearedStage < PlayerSession.Catalog.Stages.Count) {
            PlayerSession.Selection.Select(clearedStage);
        }

        unlockPopupOverlay.SetAsLastSibling();
        unlockPopupOverlay.gameObject.SetActive(true);
    }
    private void HideUnlockPopup() {
        if (unlockPopupOverlay != null) unlockPopupOverlay.gameObject.SetActive(false);
    }
    private void Update() {
        if(TutorialUI.BlocksInput)return;
        if (unlockPopupOverlay != null && unlockPopupOverlay.gameObject.activeSelf) return;
        int direction = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
        HandleNavigation(direction,Time.unscaledTime);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) TryEnter();
    }
    public void HandleNavigation(int direction, float time) {
        int move = repeat.Poll(direction,time,initialHoldDelay,repeatInterval);
        if (move < 0) PlayerSession.Selection.MovePrevious();
        else if (move > 0) PlayerSession.Selection.MoveNext();
    }
    public bool TryEnter() {
        if(TutorialUI.BlocksInput)return false;
        if(unlockPopupOverlay != null && unlockPopupOverlay.gameObject.activeSelf)return false;
        if (!Carousel.IsSettled) { feedback.text = "Wait for the selected stage to center."; return false; }
        if (!PlayerSession.TrySelectStageForPlay(out var issue)) { feedback.text = issue; return false; }
        SceneManager.LoadScene(PlayerSession.ActiveBattleScene); return true;
    }
    private void OnDisable() { repeat.Reset(); }
}
}
