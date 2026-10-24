using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Existing UI component now presents the battle Hand, not a catalog of all turret types.
public sealed class BuildMenuUI : MonoBehaviour {
    private static readonly Vector2 HandCardSize = new Vector2(84f, 132f);
    private static readonly Vector2 HandIconSize = new Vector2(45f, 64f);
    private static readonly Vector2 NextCardSize = new Vector2(74f, 116f);
    private static readonly Vector2 NextIconSize = new Vector2(40f, 56f);

    [SerializeField] private TurretPlacementController placement;
    private GameplayHand hand;
    private RuntimeCardCycle boundCycle;
    private readonly Button[] cards = new Button[3];
    private readonly Text[] labels = new Text[3];
    private readonly CardIconView[] icons = new CardIconView[3];
    private readonly CardEnergyBadge[] energyBadges = new CardEnergyBadge[3];

    private RectTransform nextHeaderRect;
    private Button nextCardView;
    private Text nextCardLabel;
    private CardIconView nextCardIcon;
    private CardEnergyBadge nextCardEnergyBadge;
    private bool interactive = true;

    public void Initialize(Transform parent) {
        hand = placement.GetComponent<GameplayHand>();
        for (int i = 0; i < 3; i++) {
            int index = i;
            cards[i] = UiFactory.Button(parent, "", new Vector2(-62f, 190f - i * 138f), () => hand.ToggleSelection(index), new Vector2(1f, .5f), HandCardSize);
            cards[i].name = "Hand Card " + (i + 1);
            cards[i].image.preserveAspect = true;
            labels[i] = cards[i].GetComponentInChildren<Text>();
            var outline = labels[i].gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, .95f);
            outline.effectDistance = new Vector2(1.2f, -1.2f);
            icons[i] = CardIconView.Create(cards[i].transform, new Vector2(0f, 8f), HandIconSize);
            CardTextBackdrop.Create((RectTransform)cards[i].transform, labels[i]);
            energyBadges[i] = CardEnergyBadge.Create((RectTransform)cards[i].transform);
        }

        var nextHeaderText = UiFactory.Label(parent, "NEXT", new Vector2(1f, .5f), new Vector2(-62f, -169f), new Vector2(90f, 22f), 14);
        nextHeaderText.name = "Next Card Header";
        nextHeaderText.font = UiFactory.BannerFont;
        nextHeaderText.fontStyle = FontStyle.Bold;
        nextHeaderText.color = new Color(1f, .93f, .72f, 1f);
        var headerOutline = nextHeaderText.gameObject.AddComponent<Outline>();
        headerOutline.effectColor = new Color(.16f, .07f, .02f, .95f);
        headerOutline.effectDistance = new Vector2(1.2f, -1.2f);
        nextHeaderRect = nextHeaderText.rectTransform;

        nextCardView = UiFactory.Button(parent, "", new Vector2(-62f, -242f), null, new Vector2(1f, .5f), NextCardSize);
        nextCardView.name = "Next Upcoming Card";
        nextCardView.interactable = false;
        nextCardView.transition = Selectable.Transition.None;
        nextCardView.image.preserveAspect = true;
        nextCardView.image.raycastTarget = false;
        nextCardLabel = nextCardView.GetComponentInChildren<Text>();
        var nextOutline = nextCardLabel.gameObject.AddComponent<Outline>();
        nextOutline.effectColor = new Color(0f, 0f, 0f, .95f);
        nextOutline.effectDistance = new Vector2(1.1f, -1.1f);
        nextCardIcon = CardIconView.Create(nextCardView.transform, new Vector2(0f, 7f), NextIconSize);
        CardTextBackdrop.Create((RectTransform)nextCardView.transform, nextCardLabel);
        nextCardEnergyBadge = CardEnergyBadge.Create((RectTransform)nextCardView.transform);

        hand.Changed += Refresh;
        Refresh();
    }

    private void BindCycleEvents() {
        if (hand == null || hand.Cycle == boundCycle) return;
        if (boundCycle != null) boundCycle.Changed -= Refresh;
        boundCycle = hand.Cycle;
        if (boundCycle != null) boundCycle.Changed += Refresh;
    }

    private void Refresh() {
        if (hand == null || hand.Cycle == null) return;
        BindCycleEvents();
        bool hasUpcoming = hand.Cycle.UpcomingQueue != null && hand.Cycle.UpcomingQueue.Count > 0;
        for (int i = 0; i < 3; i++) {
            var rect = (RectTransform)cards[i].transform;
            rect.anchoredPosition = hasUpcoming
                ? new Vector2(-62f, 190f - i * 138f)
                : new Vector2(-62f, 130f - i * 145f);

            var card = hand.Cycle.Hand[i];
            var def = card.Definition;
            bool hasCardSprite = def != null && def.CardSprite != null;
            cards[i].image.sprite = hasCardSprite ? def.CardSprite : null;
            if (hasCardSprite) {
                icons[i].gameObject.SetActive(false);
                cards[i].image.color = card == hand.SelectedCard ? new Color(.72f, 1f, .78f) : Color.white;
            } else {
                icons[i].Bind(def);
                cards[i].image.color = card == hand.SelectedCard ? new Color(.2f, .55f, .36f) : def.Color;
            }
            labels[i].text = def.DisplayName;
            energyBadges[i].Bind(def);
            cards[i].interactable = interactive;
        }

        if (nextHeaderRect != null) nextHeaderRect.gameObject.SetActive(hasUpcoming);
        if (nextCardView != null) {
            nextCardView.gameObject.SetActive(hasUpcoming);
            if (hasUpcoming) {
                var nextDef = hand.Cycle.UpcomingQueue[0].Definition;
                bool hasNextSprite = nextDef != null && nextDef.CardSprite != null;
                nextCardView.image.sprite = hasNextSprite ? nextDef.CardSprite : null;
                if (hasNextSprite) {
                    nextCardIcon.gameObject.SetActive(false);
                    nextCardView.image.color = new Color(.9f, .9f, .9f, .95f);
                } else {
                    nextCardIcon.Bind(nextDef);
                    var c = nextDef != null ? nextDef.Color : Color.gray;
                    nextCardView.image.color = new Color(c.r * .9f, c.g * .9f, c.b * .9f, .95f);
                }
                nextCardLabel.text = nextDef != null ? nextDef.DisplayName : "";
                nextCardEnergyBadge.Bind(nextDef);
            }
        }
    }

    public void SetInteractionEnabled(bool enabled) { interactive = enabled; Refresh(); }
    private void OnDestroy() {
        if (hand != null) hand.Changed -= Refresh;
        if (boundCycle != null) boundCycle.Changed -= Refresh;
    }
}
}
