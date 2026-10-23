using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class ElixirBarUI : MonoBehaviour {
    private ElixirSystem elixir;
    private Image bar;
    private readonly Sprite[] states = new Sprite[11];
    private int displayedState = -1;
    public float DisplayedFill { get; private set; }
    public void Initialize(Transform parent, ElixirSystem system) {
        elixir = system;
        for (int i=0;i<states.Length;i++) {
            states[i] = Resources.Load<Sprite>("UI/EnergyBarStates/energy_"+i.ToString("00"));
            if (states[i] == null) Debug.LogError("Missing energy bar state: "+i);
        }
        // Preserve the supplied 80 x 416 artwork's aspect ratio and pixel edges.
        var background = UiFactory.Rect("Energy Bar",parent,new Vector2(1,.5f),
            new Vector2(-168,-15),new Vector2(64,332.8f));
        bar = background.gameObject.AddComponent<Image>();
        bar.preserveAspect = true;
        bar.raycastTarget = false;
        var iconSprite = Resources.Load<Sprite>("UI/EnergyIcon");
        if (iconSprite != null) {
            var iconRect = UiFactory.Rect("Energy Icon",background,new Vector2(.5f,1),
                new Vector2(0,-5),new Vector2(38,56));
            var iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
        Advance(0);
    }
    private void Update() => Advance(Time.unscaledDeltaTime);
    public void Advance(float deltaTime) {
        if (elixir == null || bar == null) return;
        DisplayedFill = Mathf.Clamp01(elixir.Current/ElixirSystem.Maximum);
        // Whole available energy: 6.9 uses state 06; state 10 is only shown at full energy.
        // Update immediately after spending instead of showing an outdated interpolated fill.
        int state = Mathf.Clamp(Mathf.FloorToInt(DisplayedFill*10),0,10);
        if (state != displayedState) { bar.sprite = states[state]; displayedState = state; }
    }
}
}
