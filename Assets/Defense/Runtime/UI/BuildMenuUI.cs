using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class BuildMenuUI : MonoBehaviour {
    [SerializeField] private StageDefinition stage;
    [SerializeField] private TurretPlacementController placement;
    private readonly Dictionary<TurretDefinition, Button> options = new Dictionary<TurretDefinition, Button>();
    private GameObject panel;
    private Button toggle;
    private Text selectionLabel;
    public bool IsOpen => panel != null && panel.activeSelf;
    public void Initialize(Transform parent) {
        toggle = UiFactory.Button(parent, "Turrets", new Vector2(-100,40), Toggle,
            new Vector2(1,0), new Vector2(180,52));
        selectionLabel = UiFactory.Label(parent, "Selected: None", Vector2.zero,
            new Vector2(320,35), new Vector2(600,50), 22);
        float height = 65 + stage.AvailableTurrets.Count * 64;
        var rect = UiFactory.Rect("Turret Selection", parent, new Vector2(1,0),
            new Vector2(-200,85 + height/2), new Vector2(380,height));
        panel = rect.gameObject;
        panel.AddComponent<Image>().color = new Color(.08f,.12f,.18f,.98f);
        UiFactory.Label(rect,"Select Turret",new Vector2(.5f,1),new Vector2(0,-30),new Vector2(350,50));
        int index = 0;
        foreach (TurretDefinition definition in stage.AvailableTurrets) {
            TurretDefinition item = definition;
            options.Add(item, UiFactory.Button(rect, item.DisplayName + " (" + item.Cost + ")",
                new Vector2(0,-90-index*64), () => Select(item), new Vector2(.5f,1), new Vector2(350,52)));
            index++;
        }
        placement.SelectionChanged += RefreshSelection;
        RefreshSelection(placement.Selected);
        panel.SetActive(false);
    }
    private void Toggle() { panel.SetActive(!panel.activeSelf); }
    private void Select(TurretDefinition definition) {
        placement.Select(definition);
        panel.SetActive(false);
    }
    private void RefreshSelection(TurretDefinition definition) {
        selectionLabel.text = definition == null ? "Selected: None" : "Selected: " + definition.DisplayName + " (" + definition.Cost + ")";
        foreach (var option in options)
            option.Value.image.color = option.Key == definition ? new Color(.2f,.55f,.36f) : new Color(.18f,.26f,.34f);
    }
    public void SetInteractionEnabled(bool enabled) {
        toggle.interactable = enabled;
        if (!enabled) panel.SetActive(false);
    }
    private void OnDestroy() {
        if (placement != null) placement.SelectionChanged -= RefreshSelection;
    }
}
}
