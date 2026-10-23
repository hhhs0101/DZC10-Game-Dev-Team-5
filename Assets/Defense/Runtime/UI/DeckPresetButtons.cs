using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Deck Editor preset controls. UI sends intent; the manager owns selection and persistence.
public sealed class DeckPresetButtons : MonoBehaviour {
    private DeckPresetManager presets;
    private readonly Button[] buttons = new Button[DeckPresetManager.PresetCount];
    public void Initialize(DeckPresetManager model) {
        presets = model;
        for (int i=0;i<buttons.Length;i++) {
            int index = i;
            buttons[i] = UiFactory.Button(transform,"Preset "+(i+1),new Vector2((i-1)*155,0),()=>presets.SwitchPreset(index),size:new Vector2(145,42));
        }
        Bind(model);
    }
    public void Bind(DeckPresetManager model) {
        if(presets != null) presets.Changed -= Refresh;
        presets = model; presets.Changed += Refresh; Refresh();
    }
    private void Refresh() {
        for (int i=0;i<buttons.Length;i++) {
            bool selected = i==presets.ActivePresetIndex;
            buttons[i].image.color = selected ? new Color(.2f,.55f,.36f) : new Color(.18f,.26f,.34f);
            buttons[i].interactable = !selected;
        }
    }
    private void OnDestroy() { if (presets != null) presets.Changed -= Refresh; }
}
}
