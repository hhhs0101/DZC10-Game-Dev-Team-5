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
    private RectTransform popupOverlay;
    private Image popupCardImage;
    private CardIconView popupCardIcon;
    private CardEnergyBadge popupEnergyBadge;
    private Text popupCardLabel;
    private Text popupTitleText;
    private Text popupRoleText;
    private Text popupStatsText;
    private Text popupAbilityText;
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
            card.gameObject.AddComponent<DeckCardDrag>().Initialize(definition,canvas,ShowCardPopup);
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
            drops[i] = card.gameObject.AddComponent<DeckSlotDrop>();
            drops[i].Initialize(editor,i,OnDeckSlotClicked);
        }
        var instructionLabel = UiFactory.Label(transform,"Drag an available card onto a deck slot, or click any card to view its stats.",new Vector2(.5f,0),new Vector2(0,35),new Vector2(1100,50),20);
        var instructionOutline = instructionLabel.gameObject.AddComponent<Outline>();
        instructionOutline.effectColor = new Color(0f,0f,0f,.95f);
        instructionOutline.effectDistance = new Vector2(1.5f,-1.5f);
        warning = UiFactory.Label(transform,"",new Vector2(.5f,0),new Vector2(0,80),new Vector2(1100,45),22).gameObject.AddComponent<FadingMessageUI>();
        BuildPopup();
        deck.Rejected += ShowWarning;
        presets.Changed += OnPresetsChanged; PlayerSession.StageCardsChanged += OnStageChanged; Refresh();
    }
    private void OnDeckSlotClicked(int index) {
        if (deck == null || index < 0 || index >= deck.Slots.Count) return;
        ShowCardPopup(deck.Slots[index]);
    }
    private void BuildPopup() {
        popupOverlay = UiFactory.Rect("Tower Stats Popup Overlay",transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        popupOverlay.anchorMin = Vector2.zero;
        popupOverlay.anchorMax = Vector2.one;
        popupOverlay.offsetMin = popupOverlay.offsetMax = Vector2.zero;
        var dimmerImage = popupOverlay.gameObject.AddComponent<Image>();
        dimmerImage.color = new Color(0f,0f,0f,.68f);
        var dimmerButton = popupOverlay.gameObject.AddComponent<Button>();
        dimmerButton.transition = Selectable.Transition.None;
        dimmerButton.onClick.AddListener(GameAudio.PlayButtonClick);
        dimmerButton.onClick.AddListener(HideCardPopup);

        var dialog = UiFactory.Rect("Tower Stats Dialog",popupOverlay,new Vector2(.5f,.5f),Vector2.zero,new Vector2(680,420));
        var dialogBg = dialog.gameObject.AddComponent<Image>();
        var woodSprite = Resources.Load<Sprite>("UI/TowerCardsWoodPanel");
        if (woodSprite != null) {
            dialogBg.sprite = woodSprite;
            dialogBg.color = Color.white;
        } else {
            dialogBg.color = new Color(.22f,.15f,.1f,.98f);
        }
        dialogBg.raycastTarget = true;

        var innerPanel = UiFactory.Rect("Inner Content Backdrop",dialog,new Vector2(.5f,.5f),new Vector2(0,-6),new Vector2(620,348));
        var innerImage = innerPanel.gameObject.AddComponent<Image>();
        innerImage.color = new Color(.12f,.08f,.05f,.82f);
        innerImage.raycastTarget = false;

        var previewCard = UiFactory.Rect("Popup Card Preview",dialog,new Vector2(0f,.5f),new Vector2(126,12),new Vector2(150,232));
        popupCardImage = previewCard.gameObject.AddComponent<Image>();
        popupCardImage.preserveAspect = true;
        popupCardImage.raycastTarget = false;
        popupCardIcon = CardIconView.Create(previewCard,new Vector2(0,16),new Vector2(76,118));
        popupCardLabel = UiFactory.Label(previewCard,"",new Vector2(.5f,.5f),new Vector2(0,-72),new Vector2(138,54),15);
        var cardOutline = popupCardLabel.gameObject.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0f,0f,0f,.95f);
        cardOutline.effectDistance = new Vector2(1.2f,-1.2f);
        CardTextBackdrop.Create(previewCard,popupCardLabel);
        popupEnergyBadge = CardEnergyBadge.Create(previewCard);

        popupTitleText = UiFactory.Label(dialog,"",new Vector2(0f,1f),new Vector2(405,-58),new Vector2(390,44),30);
        popupTitleText.alignment = TextAnchor.MiddleLeft;
        popupTitleText.fontStyle = FontStyle.Bold;
        popupTitleText.color = new Color(1f,.92f,.65f);
        var titleOutline = popupTitleText.gameObject.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0f,0f,0f,.95f);
        titleOutline.effectDistance = new Vector2(1.5f,-1.5f);

        popupRoleText = UiFactory.Label(dialog,"",new Vector2(0f,1f),new Vector2(405,-94),new Vector2(390,30),18);
        popupRoleText.alignment = TextAnchor.MiddleLeft;
        popupRoleText.color = new Color(.75f,.92f,1f);
        var roleOutline = popupRoleText.gameObject.AddComponent<Outline>();
        roleOutline.effectColor = new Color(0f,0f,0f,.95f);
        roleOutline.effectDistance = new Vector2(1.2f,-1.2f);

        popupStatsText = UiFactory.Label(dialog,"",new Vector2(0f,1f),new Vector2(415,-185),new Vector2(410,145),19);
        popupStatsText.alignment = TextAnchor.UpperLeft;
        popupStatsText.lineSpacing = 1.18f;
        var statsOutline = popupStatsText.gameObject.AddComponent<Outline>();
        statsOutline.effectColor = new Color(0f,0f,0f,.95f);
        statsOutline.effectDistance = new Vector2(1.2f,-1.2f);

        var abilityBox = UiFactory.Rect("Ability Box",dialog,new Vector2(0f,0f),new Vector2(415,105),new Vector2(410,92));
        var abilityBg = abilityBox.gameObject.AddComponent<Image>();
        abilityBg.color = new Color(.2f,.14f,.09f,.9f);
        abilityBg.raycastTarget = false;

        popupAbilityText = UiFactory.Label(abilityBox,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(390,80),17);
        popupAbilityText.alignment = TextAnchor.MiddleLeft;
        popupAbilityText.color = new Color(1f,.95f,.82f);
        var abilityOutline = popupAbilityText.gameObject.AddComponent<Outline>();
        abilityOutline.effectColor = new Color(0f,0f,0f,.95f);
        abilityOutline.effectDistance = new Vector2(1.2f,-1.2f);

        var closeBtn = UiFactory.Button(dialog,"Close",new Vector2(0,36),HideCardPopup,new Vector2(.5f,0f),new Vector2(176,54));
        if (!UiFactory.ApplyCloseButtonSkin(closeBtn))
            closeBtn.GetComponent<Image>().color = new Color(.55f,.24f,.14f,.95f);

        popupOverlay.gameObject.SetActive(false);
    }
    private void ShowCardPopup(CardDefinition card) {
        if (card == null || popupOverlay == null) return;
        popupEnergyBadge.Bind(card);
        bool hasCardSprite = card.CardSprite != null;
        popupCardImage.sprite = hasCardSprite ? card.CardSprite : null;
        popupCardImage.color = hasCardSprite ? Color.white : new Color(.2f,.32f,.4f);
        if (hasCardSprite) {
            popupCardIcon.gameObject.SetActive(false);
        } else {
            popupCardIcon.Bind(card);
        }
        popupCardLabel.text = card.DisplayName;
        popupTitleText.text = card.DisplayName;

        if (card is TowerCardDefinition towerCard && towerCard.Tower != null) {
            var t = towerCard.Tower;
            int lifetimeSeconds = Mathf.RoundToInt(TowerLifetime.DurationForCost(card.ElixirCost));
            popupRoleText.text = GetTowerRoleTitle(t);
            if (t.TowerType == TowerType.Mayonnaise) {
                popupStatsText.text =
                    "• Energy Cost: " + card.ElixirCost + "\n" +
                    "• Duration: " + lifetimeSeconds + "s\n" +
                    "• Damage Buff: +" + Mathf.RoundToInt((t.DamageBoost - 1f) * 100f) + "%\n" +
                    "• Cleanse Interval: " + t.CleanseInterval.ToString("0.##") + "s\n" +
                    "• Support Range: " + t.AttackRange.ToString("0.##");
            } else {
                float dps = t.AttackCooldown > 0f ? t.Damage / t.AttackCooldown : 0f;
                popupStatsText.text =
                    "• Energy Cost: " + card.ElixirCost + "\n" +
                    "• Duration: " + lifetimeSeconds + "s\n" +
                    "• Damage: " + t.Damage.ToString("0.##") + "  (DPS: " + dps.ToString("0.1") + "/s)\n" +
                    "• Attack Cooldown: " + t.AttackCooldown.ToString("0.##") + "s\n" +
                    "• Attack Range: " + t.AttackRange.ToString("0.##");
            }
            popupAbilityText.text = GetTowerAbilityDescription(t);
        } else if (card is SkillCardDefinition skillCard && skillCard.Skill != null) {
            var s = skillCard.Skill;
            popupRoleText.text = "Skill Card";
            popupStatsText.text =
                "• Energy Cost: " + card.ElixirCost + "\n" +
                "• Damage: " + s.Damage.ToString("0.##") + "\n" +
                "• Radius: " + s.Radius.ToString("0.##");
            popupAbilityText.text = s.ActiveDuration > 0f
                ? "Special Effect: Creates a slow zone for " + s.ActiveDuration.ToString("0.#") + "s in the target area."
                : "Special Effect: Deals instant AoE damage to enemies in the target area.";
        }
        popupOverlay.SetAsLastSibling();
        popupOverlay.gameObject.SetActive(true);
    }
    private void HideCardPopup() {
        if (popupOverlay != null) popupOverlay.gameObject.SetActive(false);
    }
    private static string GetTowerRoleTitle(TurretDefinition tower) {
        switch (tower.TowerType) {
            case TowerType.Ketchup: return "Splash AoE Tower";
            case TowerType.Mustard: return "Shield Piercing Tower";
            case TowerType.HotSauce: return "Burn DoT Tower";
            case TowerType.Oil: return "Slow Debuff Tower";
            case TowerType.Mayonnaise: return "Buff & Cleanse Support Tower";
            case TowerType.SoySauce: return "Long-Range Sniper Tower";
            case TowerType.BlackPepper: return "Rapid-Fire Striker Tower";
            case TowerType.Salt: return "Balanced Single-Target Tower";
            default:
                return tower.DisplayName == "Paprika Powder"
                    ? "Long-Range Heavy Attacker"
                    : "Single-Target Tower";
        }
    }
    private static string GetTowerAbilityDescription(TurretDefinition t) {
        switch (t.TowerType) {
            case TowerType.Ketchup:
                return "Special Ability: Deals splash damage to all enemies within a radius of " + t.SplashRadius.ToString("0.#") + " around the target.";
            case TowerType.Mustard:
                return "Special Ability: Pierces through enemy shields (such as Bell Pepper shields) to deal full damage.";
            case TowerType.HotSauce:
                return "Special Ability: Inflicts " + t.BurnDamagePerSecond.ToString("0.#") + " burn damage per second for " + t.BurnDuration.ToString("0.#") + "s on hit.";
            case TowerType.Oil:
                return "Special Ability: Slows target movement speed by " + Mathf.RoundToInt((1f - t.SlowMultiplier) * 100f) + "% for " + t.SlowDuration.ToString("0.#") + "s on hit.";
            case TowerType.Mayonnaise:
                return "Special Ability: Boosts nearby tower damage by +" + Mathf.RoundToInt((t.DamageBoost - 1f) * 100f) + "% and cleanses disrupted status every " + t.CleanseInterval.ToString("0.#") + "s.";
            case TowerType.SoySauce:
                return "Special Ability: Snipes distant enemies with a long attack range (" + t.AttackRange.ToString("0.#") + ") and high single-shot damage.";
            case TowerType.BlackPepper:
                return "Special Ability: Low energy cost (2) and fast attack rate to quickly eliminate early enemies.";
            case TowerType.Salt:
                return "Special Ability: Reliable single-target attacker with high DPS for a low energy cost (2).";
            default:
                if (t.DisplayName == "Paprika Powder")
                    return "Special Ability: Delivers heavy single-target hits from a long range (" + t.AttackRange.ToString("0.#") + ").";
                return "Special Ability: Attacks the closest enemy within range.";
        }
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
            for (int i=0;i<drops.Length;i++) drops[i].Initialize(editor,i,OnDeckSlotClicked);
            content.anchoredPosition = Vector2.zero;
            warning.Show("",0);
            HideCardPopup();
        }
        Refresh();
    }
    private void OnDisable() => HideCardPopup();
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
