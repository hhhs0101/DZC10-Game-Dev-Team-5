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
    private Text selection;
    private bool interactive = true;
    public void Initialize(Transform parent) {
        hand = placement.GetComponent<GameplayHand>();
        for (int i=0;i<3;i++) {
            int index = i;
            cards[i] = UiFactory.Button(parent,"",new Vector2((i-1)*260,57),()=>hand.ToggleSelection(index),new Vector2(.5f,0),new Vector2(245,86));
            cards[i].name = "Hand Card "+(i+1); labels[i] = cards[i].GetComponentInChildren<Text>();
            icons[i] = CardIconView.Create(cards[i].transform,new Vector2(-94,0),new Vector2(42,68));
        }
        selection = UiFactory.Label(parent,"",new Vector2(.5f,0),new Vector2(0,124),new Vector2(850,36),20);
        hand.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        for (int i=0;i<3;i++) {
            var card = hand.Cycle.Hand[i];
            bool artwork = icons[i].Bind(card.Definition);
            labels[i].rectTransform.anchoredPosition = artwork ? new Vector2(27,0) : Vector2.zero;
            labels[i].rectTransform.sizeDelta = artwork ? new Vector2(180,80) : new Vector2(245,86);
            labels[i].fontSize = artwork ? 18 : 22;
            labels[i].text = card.Definition.DisplayName+"\n"+card.Definition.CostDescription;
            cards[i].image.color = card == hand.SelectedCard ? new Color(.2f,.55f,.36f) : card.Definition.Color;
            cards[i].interactable = interactive;
        }
        selection.text = hand.SelectedCard == null ? "Select a card: Tower → click a slot. Skill → hold and release on the table." : "Selected: "+hand.SelectedCard.Definition.DisplayName;
    }
    public void SetInteractionEnabled(bool enabled) { interactive = enabled; Refresh(); }
    private void OnDestroy() { if (hand != null) hand.Changed -= Refresh; }
}
}
