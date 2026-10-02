using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Existing UI component now presents the battle Hand, not a catalog of all turret types.
public sealed class BuildMenuUI : MonoBehaviour {
    [SerializeField] private TurretPlacementController placement;
    private GameplayHand hand;
    private readonly Button[] cards = new Button[3];
    private readonly Text[] labels = new Text[3];
    private Text selection;
    private bool interactive = true;
    public void Initialize(Transform parent) {
        hand = placement.GetComponent<GameplayHand>();
        for (int i=0;i<3;i++) {
            int index = i;
            cards[i] = UiFactory.Button(parent,"",new Vector2((i-1)*260,57),()=>hand.Select(index),new Vector2(.5f,0),new Vector2(245,86));
            cards[i].name = "Hand Card "+(i+1); labels[i] = cards[i].GetComponentInChildren<Text>();
        }
        selection = UiFactory.Label(parent,"",new Vector2(.5f,0),new Vector2(0,124),new Vector2(850,36),20);
        hand.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        for (int i=0;i<3;i++) {
            var card = hand.Cycle.Hand[i]; labels[i].text = card.Definition.DisplayName+"\n"+card.Definition.CostDescription;
            cards[i].image.color = card == hand.SelectedCard ? new Color(.2f,.55f,.36f) : card.Definition.Color;
            cards[i].interactable = interactive;
        }
        selection.text = hand.SelectedCard == null ? "Select a card." : "Selected: "+hand.SelectedCard.Definition.DisplayName;
    }
    public void SetInteractionEnabled(bool enabled) { interactive = enabled; Refresh(); }
    private void OnDestroy() { if (hand != null) hand.Changed -= Refresh; }
}
}
