using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Existing UI component now presents the battle Hand, not a catalog of all turret types.
public sealed class BuildMenuUI : MonoBehaviour {
    [SerializeField] private TurretPlacementController placement;
    private GameplayHand hand;
    private readonly Button[] cards = new Button[3];
    private readonly Text[] labels = new Text[3];
    private readonly CardIconView[] icons = new CardIconView[3];
    private readonly CardEnergyBadge[] energyBadges = new CardEnergyBadge[3];
    private Text selection;
    private bool interactive = true;
    public void Initialize(Transform parent) {
        hand = placement.GetComponent<GameplayHand>();
        for (int i=0;i<3;i++) {
            int index = i;
            cards[i] = UiFactory.Button(parent,"",new Vector2(-62,145-i*175),()=>hand.ToggleSelection(index),new Vector2(1,.5f),new Vector2(104,164));
            cards[i].name = "Hand Card "+(i+1);
            cards[i].image.preserveAspect = true;
            labels[i] = cards[i].GetComponentInChildren<Text>();
            var outline = labels[i].gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f,0f,0f,.95f);
            outline.effectDistance = new Vector2(1.2f,-1.2f);
            icons[i] = CardIconView.Create(cards[i].transform,new Vector2(0,10),new Vector2(56,80));
            CardTextBackdrop.Create((RectTransform)cards[i].transform,labels[i]);
            energyBadges[i] = CardEnergyBadge.Create((RectTransform)cards[i].transform);
        }
        selection = UiFactory.Label(parent,"",new Vector2(1,.5f),new Vector2(-110,252),new Vector2(204,36),14);
        var selectionOutline = selection.gameObject.AddComponent<Outline>();
        selectionOutline.effectColor = new Color(0f,0f,0f,.95f);
        selectionOutline.effectDistance = new Vector2(1.2f,-1.2f);
        hand.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        for (int i=0;i<3;i++) {
            var card = hand.Cycle.Hand[i];
            var def = card.Definition;
            bool hasCardSprite = def != null && def.CardSprite != null;
            cards[i].image.sprite = hasCardSprite ? def.CardSprite : null;
            if (hasCardSprite) {
                icons[i].gameObject.SetActive(false);



                cards[i].image.color = card == hand.SelectedCard ? new Color(.72f,1f,.78f) : Color.white;
            } else {
                bool artwork = icons[i].Bind(def);



                cards[i].image.color = card == hand.SelectedCard ? new Color(.2f,.55f,.36f) : def.Color;
            }
            labels[i].text = def.DisplayName;
            energyBadges[i].Bind(def);
            cards[i].interactable = interactive;
        }
        selection.text = hand.SelectedCard == null ? "" : "Selected: "+hand.SelectedCard.Definition.DisplayName;
    }
    public void SetInteractionEnabled(bool enabled) { interactive = enabled; Refresh(); }
    private void OnDestroy() { if (hand != null) hand.Changed -= Refresh; }
}
}
