using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class LobbyDeckPreview : MonoBehaviour {
    private DeckPresetManager presets;
    private readonly Text[] labels = new Text[PlayerDeck.SlotCount];
    public void Initialize(DeckPresetManager model) {
        presets = model;
        for (int i=0;i<labels.Length;i++) {
            var rect = UiFactory.Rect("Deck Preview T"+(i+1),transform,new Vector2(.5f,.5f),new Vector2((i-2.5f)*155,0),new Vector2(145,105));
            rect.gameObject.AddComponent<Image>().color = new Color(.16f,.25f,.3f);
            labels[i] = UiFactory.Label(rect,"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(140,100),18);
        }
        presets.Changed += Refresh; Refresh();
    }
    private void Refresh() { for (int i=0;i<labels.Length;i++) labels[i].text = "T"+(i+1)+"\n"+presets.ActiveDeck.Slots[i].DisplayName; }
    private void OnDestroy() { if (presets != null) presets.Changed -= Refresh; }
}
}
