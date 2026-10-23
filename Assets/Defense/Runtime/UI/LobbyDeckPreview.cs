using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class LobbyDeckPreview : MonoBehaviour {
    private DeckPresetManager presets;
    private readonly Image[] images = new Image[PlayerDeck.SlotCount];
    private readonly Text[] labels = new Text[PlayerDeck.SlotCount];
    private readonly CardIconView[] icons = new CardIconView[PlayerDeck.SlotCount];
    public void Initialize(DeckPresetManager model) {
        presets = model;
        for (int i=0;i<labels.Length;i++) {
            var rect = UiFactory.Rect("Deck Preview T"+(i+1),transform,new Vector2(.5f,.5f),new Vector2((i-2.5f)*106,4),new Vector2(80,126));
            images[i] = rect.gameObject.AddComponent<Image>();
            images[i].preserveAspect = true;
            labels[i] = UiFactory.Label(rect,"",new Vector2(.5f,.5f),new Vector2(0,-40),new Vector2(76,38),12);
            var outline = labels[i].gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f,0f,0f,.95f);
            outline.effectDistance = new Vector2(1.2f,-1.2f);
            icons[i] = CardIconView.Create(rect,new Vector2(0,8),new Vector2(44,68));
        }
        presets.Changed += Refresh; PlayerSession.StageCardsChanged += OnStageChanged; Refresh();
    }
    private void OnStageChanged() {
        if(presets != null) presets.Changed -= Refresh;
        presets = PlayerSession.Presets; presets.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        for (int i=0;i<labels.Length;i++) {
            bool occupied = i < presets.ActiveDeck.Slots.Count;
            images[i].gameObject.SetActive(occupied); if(!occupied) continue;
            images[i].rectTransform.anchoredPosition = new Vector2((i-(presets.ActiveDeck.Slots.Count-1)*.5f)*106,4);
            var card = presets.ActiveDeck.Slots[i];
            bool hasCardSprite = card != null && card.CardSprite != null;
            images[i].sprite = hasCardSprite ? card.CardSprite : null;
            images[i].color = hasCardSprite ? Color.white : new Color(.16f,.25f,.3f);
            if (hasCardSprite) {
                icons[i].gameObject.SetActive(false);
                labels[i].rectTransform.anchoredPosition = new Vector2(0,-40);
                labels[i].rectTransform.sizeDelta = new Vector2(76,38);
                labels[i].fontSize = 12;
            } else {
                bool artwork = icons[i].Bind(card);
                labels[i].rectTransform.anchoredPosition = artwork ? new Vector2(0,-38) : Vector2.zero;
                labels[i].rectTransform.sizeDelta = new Vector2(76,42);
                labels[i].fontSize = 13;
            }
            labels[i].text = "T"+(i+1)+"\n"+card.DisplayName;
        }
    }
    private void OnDestroy() { PlayerSession.StageCardsChanged -= OnStageChanged; if (presets != null) presets.Changed -= Refresh; }
}
}
