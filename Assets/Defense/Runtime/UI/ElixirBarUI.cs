using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class ElixirBarUI : MonoBehaviour {
    private ElixirSystem elixir;
    private RectTransform fill;
    private Text label;
    public float DisplayedFill { get; private set; }
    public void Initialize(Transform parent, ElixirSystem system) {
        elixir = system; DisplayedFill = elixir.Current/ElixirSystem.Maximum;
        var background = UiFactory.Rect("Elixir Bar",parent,new Vector2(1,.5f),new Vector2(-60,0),new Vector2(42,230));
        background.gameObject.AddComponent<Image>().color = new Color(.15f,.1f,.2f);
        fill = UiFactory.Rect("Elixir Fill",background,Vector2.zero,Vector2.zero,Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(1,DisplayedFill); fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(.65f,.3f,.9f);
        label = UiFactory.Label(parent,"",new Vector2(1,.5f),new Vector2(-60,-150),new Vector2(110,55),18);
    }
    private void Update() => Advance(Time.unscaledDeltaTime);
    public void Advance(float deltaTime) {
        if (elixir == null) return;
        DisplayedFill = Mathf.MoveTowards(DisplayedFill,elixir.Current/ElixirSystem.Maximum,Mathf.Max(0,deltaTime)*.5f);
        fill.anchorMax = new Vector2(1,DisplayedFill);
        label.text = "Elixir\n"+elixir.Current.ToString("0.0")+" / 10";
    }
}
}
