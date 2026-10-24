using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class DeckEditorUI : MonoBehaviour {
    private DeckPresetManager presets;
    private readonly CardEnergyBadge[] energyBadges = new CardEnergyBadge[6];
    private Text stageTitle, emptyPool;
    private PlayerDeck deck;
    private readonly DeckSlotDrop[] drops = new DeckSlotDrop[PlayerDeck.SlotCount];
    private FadingMessageUI warning;
    private DeckEditorController editor;
    private RectTransform content;
    private readonly Dictionary<CardDefinition, RectTransform> availableViews = new Dictionary<CardDefinition, RectTransform>();
    private readonly Image[] slotImages = new Image[6];
    private readonly Text[] labels = new Text[6];
    private readonly CardIconView[] icons = new CardIconView[6];
    public void Initialize(RectTransform canvas, Action back, Action settings, MainMenuSkin skin = null) {
        presets = PlayerSession.Presets;
        deck = presets.ActiveDeck;
        editor = new DeckEditorController(deck,PlayerSession.AvailableCards);

        var panelImage = GetComponent<Image>();
        if (panelImage != null) panelImage.color = Color.clear;
        var bg = UiFactory.Rect("Deck Background",transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        bg.anchorMin = Vector2.zero; bg.anchorMax = Vector2.one;
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.SetAsFirstSibling();
        var bgImage = bg.gameObject.AddComponent<Image>();
        var bgSprite = Resources.Load<Sprite>("UI/DeckBackground");
        bgImage.sprite = bgSprite;
        bgImage.preserveAspect = true;
        bgImage.raycastTarget = false;
        var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectRatio = bgSprite != null ? bgSprite.rect.width / bgSprite.rect.height : 16f / 9f;
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

        var backButton = UiFactory.Button(transform,"Back to Lobby",new Vector2(140,-40),()=>back(),new Vector2(0,1),new Vector2(250,48));
        var settingsButton = UiFactory.Button(transform,"Settings",new Vector2(-100,-40),()=>settings(),new Vector2(1,1),new Vector2(180,48));
        if (skin != null) {
            UiFactory.ApplyButtonSkin(backButton,skin.LobbyBack);
            UiFactory.ApplyButtonSkin(settingsButton,skin.LobbySettings);
        }
        var leftPanel = UiFactory.Rect("Tower Cards Wood Panel",transform,new Vector2(.25f,1),new Vector2(0,-335),new Vector2(590,442));
        var leftPanelImage = leftPanel.gameObject.AddComponent<Image>();
        leftPanelImage.sprite = Resources.Load<Sprite>("UI/TowerCardsWoodPanel");
        leftPanelImage.preserveAspect = false;
        leftPanelImage.raycastTarget = false;

        var rightBoard = UiFactory.Rect("Six Card Cutting Board",transform,new Vector2(.75f,1),new Vector2(0,-335),new Vector2(590,442));
        var rightBoardImage = rightBoard.gameObject.AddComponent<Image>();
        rightBoardImage.sprite = Resources.Load<Sprite>("UI/SixCardCuttingBoard");
        rightBoardImage.preserveAspect = true;
        rightBoardImage.raycastTarget = false;

        stageTitle = UiFactory.Label(transform,"Available Cards",new Vector2(.25f,1),new Vector2(0,-82),new Vector2(500,55),30);
        var stageTitleOutline = stageTitle.gameObject.AddComponent<Outline>();
        stageTitleOutline.effectColor = new Color(0f,0f,0f,.95f);
        stageTitleOutline.effectDistance = new Vector2(1.5f,-1.5f);

        var currentDeckLabel = UiFactory.Label(transform,"Current Deck",new Vector2(.75f,1),new Vector2(0,-82),new Vector2(500,55),30);
        var currentDeckOutline = currentDeckLabel.gameObject.AddComponent<Outline>();
        currentDeckOutline.effectColor = new Color(0f,0f,0f,.95f);
        currentDeckOutline.effectDistance = new Vector2(1.5f,-1.5f);

        var viewport = UiFactory.Rect("Available Cards List",transform,new Vector2(.25f,1),new Vector2(0,-335),new Vector2(520,368));
        var viewportImage = viewport.gameObject.AddComponent<Image>();
        viewportImage.color = Color.clear;
        viewport.gameObject.AddComponent<RectMask2D>();
        emptyPool = UiFactory.Label(viewport,"All cards for this stage are equipped.",new Vector2(.5f,.5f),Vector2.zero,new Vector2(450,90),22);
        var emptyOutline = emptyPool.gameObject.AddComponent<Outline>();
        emptyOutline.effectColor = new Color(0f,0f,0f,.95f);
        emptyOutline.effectDistance = new Vector2(1.2f,-1.2f);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        content = UiFactory.Rect("Available Cards",viewport,new Vector2(.5f,1),Vector2.zero,new Vector2(520,Mathf.Max(368,Mathf.CeilToInt(editor.Available.Count/3f)*175+18)));
        content.pivot = new Vector2(.5f,1); scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        for (int i=0;i<PlayerSession.Catalog.AvailableCards.Count;i++) {
            var definition = PlayerSession.Catalog.AvailableCards[i];
            if (definition == null || availableViews.ContainsKey(definition)) continue;
            var card = UiFactory.Rect("Available "+definition.DisplayName,content,new Vector2(.5f,1),new Vector2((i%3-1)*155,-96-i/3*175),new Vector2(106,164));
            var cardImage = card.gameObject.AddComponent<Image>();
            cardImage.preserveAspect = true;
            bool hasCardSprite = definition.CardSprite != null;
            cardImage.sprite = hasCardSprite ? definition.CardSprite : null;
            cardImage.color = hasCardSprite ? Color.white : new Color(.2f,.32f,.4f);
            var iconView = CardIconView.Create(card,new Vector2(0,12),new Vector2(54,84));
            if (hasCardSprite) iconView.gameObject.SetActive(false); else iconView.Bind(definition);
            var label = UiFactory.Label(card,definition.DisplayName,new Vector2(.5f,.5f),new Vector2(0,-46),new Vector2(100,64),12);
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f,0f,0f,.95f);
            outline.effectDistance = new Vector2(1.2f,-1.2f);
            card.gameObject.AddComponent<DeckCardDrag>().Initialize(definition,canvas);
            CardTextBackdrop.Create(card,label);
            CardEnergyBadge.Create(card).Bind(definition);
            availableViews.Add(definition,card);
        }
        for (int i=0;i<6;i++) {
            var card = UiFactory.Rect("Deck Slot T"+(i+1),transform,new Vector2(.75f,1),new Vector2((i%3-1)*150,-235-i/3*195),new Vector2(114,176));
            slotImages[i] = card.gameObject.AddComponent<Image>();
            slotImages[i].preserveAspect = true;
            labels[i] = UiFactory.Label(card,"",new Vector2(.5f,.5f),new Vector2(0,-56),new Vector2(108,48),14);
            var outline = labels[i].gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f,0f,0f,.95f);
            outline.effectDistance = new Vector2(1.2f,-1.2f);
            icons[i] = CardIconView.Create(card,new Vector2(0,12),new Vector2(58,90));
            CardTextBackdrop.Create(card,labels[i]);
            energyBadges[i] = CardEnergyBadge.Create(card);
            drops[i] = card.gameObject.AddComponent<DeckSlotDrop>(); drops[i].Initialize(editor,i);
        }
        var instructionLabel = UiFactory.Label(transform,"Drag an available card onto a deck slot. Changes save automatically.",new Vector2(.5f,0),new Vector2(0,35),new Vector2(1100,50),20);
        var instructionOutline = instructionLabel.gameObject.AddComponent<Outline>();
        instructionOutline.effectColor = new Color(0f,0f,0f,.95f);
        instructionOutline.effectDistance = new Vector2(1.5f,-1.5f);
        warning = UiFactory.Label(transform,"",new Vector2(.5f,0),new Vector2(0,80),new Vector2(1100,45),22).gameObject.AddComponent<FadingMessageUI>();
        deck.Rejected += ShowWarning;
        presets.Changed += OnPresetsChanged; PlayerSession.StageCardsChanged += OnStageChanged; Refresh();
    }
    private void OnStageChanged() {
        if(presets != null) presets.Changed -= OnPresetsChanged;
        presets = PlayerSession.Presets; presets.Changed += OnPresetsChanged;
        OnPresetsChanged();
    }
    private void OnPresetsChanged() {
        if (deck != presets.ActiveDeck) {
            foreach (var view in availableViews.Values) view.GetComponent<DeckCardDrag>().Cancel();
            deck.Rejected -= ShowWarning; deck = presets.ActiveDeck; deck.Rejected += ShowWarning;
            editor = new DeckEditorController(deck,PlayerSession.AvailableCards);
            for (int i=0;i<drops.Length;i++) drops[i].Initialize(editor,i);
            content.anchoredPosition = Vector2.zero;
            warning.Show("",0);
        }
        Refresh();
    }
    private void Refresh() {
        stageTitle.text = (PlayerSession.CardStage != null ? PlayerSession.CardStage.DisplayName : "All") + " Cards";
        for (int i=0;i<6;i++) {
            bool occupied = i < deck.Slots.Count;
            slotImages[i].gameObject.SetActive(occupied); if(!occupied) continue;
            var slotCard = deck.Slots[i];
            energyBadges[i].Bind(slotCard);
            bool hasCardSprite = slotCard != null && slotCard.CardSprite != null;
            slotImages[i].sprite = hasCardSprite ? slotCard.CardSprite : null;
            slotImages[i].color = hasCardSprite ? Color.white : new Color(.2f,.28f,.25f);
            if (hasCardSprite) {
                icons[i].gameObject.SetActive(false);



            } else {
                bool artwork = icons[i].Bind(slotCard);



            }
            labels[i].text = "T"+(i+1)+"\n"+slotCard.DisplayName;
        }
        var available = editor.Available;
        emptyPool.gameObject.SetActive(available.Count == 0);
        var visible = new HashSet<CardDefinition>(available);
        // Reuse views. Hiding a consumed source safely cancels its drag ghost during OnDrop.
        foreach (var pair in availableViews) pair.Value.gameObject.SetActive(visible.Contains(pair.Key));
        for (int i=0;i<available.Count;i++)
            availableViews[available[i]].anchoredPosition = new Vector2((i%3-1)*155,-96-i/3*175);
        content.sizeDelta = new Vector2(520,Mathf.Max(368,Mathf.CeilToInt(available.Count/3f)*175+18));
    }
    private void ShowWarning(string message) => warning.Show(message,3);
    private void OnDestroy() { PlayerSession.StageCardsChanged -= OnStageChanged; if (presets != null) presets.Changed -= OnPresetsChanged; if (deck != null) deck.Rejected -= ShowWarning; }
}
}
