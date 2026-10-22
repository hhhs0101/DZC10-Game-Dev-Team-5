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
        var iconSprite = Resources.Load<Sprite>("UI/EnergyIcon");
        if (iconSprite != null) {
            var iconRect = UiFactory.Rect("Energy Icon",parent,new Vector2(1,.5f),new Vector2(-168,185),new Vector2(38,56));
            var iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }
        var background = UiFactory.Rect("Energy Bar",parent,new Vector2(1,.5f),new Vector2(-168,-15),new Vector2(34,310));
        background.gameObject.AddComponent<Image>().color = new Color(.14f,.11f,.06f);
        fill = UiFactory.Rect("Energy Fill",background,Vector2.zero,Vector2.zero,Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(1,DisplayedFill); fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(1f,.84f,.12f);
        label = UiFactory.Label(parent,"",new Vector2(1,.5f),new Vector2(-168,-202),new Vector2(92,48),15);
    }
    private void Update() => Advance(Time.unscaledDeltaTime);
    public void Advance(float deltaTime) {
        if (elixir == null) return;
        DisplayedFill = Mathf.MoveTowards(DisplayedFill,elixir.Current/ElixirSystem.Maximum,Mathf.Max(0,deltaTime)*.5f);
        fill.anchorMax = new Vector2(1,DisplayedFill);
        label.text = "Energy\n"+elixir.Current.ToString("0.0")+" / 10";
    }
}
}
