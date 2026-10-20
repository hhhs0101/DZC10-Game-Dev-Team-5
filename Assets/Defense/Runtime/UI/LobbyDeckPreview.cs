using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class LobbyDeckPreview : MonoBehaviour {
    private DeckPresetManager presets;
    private readonly Text[] labels = new Text[PlayerDeck.SlotCount];
    private readonly CardIconView[] icons = new CardIconView[PlayerDeck.SlotCount];
    public void Initialize(DeckPresetManager model) {
        presets = model;
        for (int i=0;i<labels.Length;i++) {
            var rect = UiFactory.Rect("Deck Preview T"+(i+1),transform,new Vector2(.5f,.5f),new Vector2((i-2.5f)*155,0),new Vector2(145,105));
            rect.gameObject.AddComponent<Image>().color = new Color(.16f,.25f,.3f);
            labels[i] = UiFactory.Label(rect,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(140,100),18);
            icons[i] = CardIconView.Create(rect,new Vector2(-48,0),new Vector2(36,72));
        }
        presets.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        for (int i=0;i<labels.Length;i++) {
            var card = presets.ActiveDeck.Slots[i]; bool artwork = icons[i].Bind(card);
            labels[i].rectTransform.anchoredPosition = artwork ? new Vector2(20,0) : Vector2.zero;
            labels[i].rectTransform.sizeDelta = artwork ? new Vector2(96,100) : new Vector2(140,100);
            labels[i].fontSize = artwork ? 16 : 18;
            labels[i].text = "T"+(i+1)+"\n"+card.DisplayName;
        }
    }
    private void OnDestroy() { if (presets != null) presets.Changed -= Refresh; }
}
}
