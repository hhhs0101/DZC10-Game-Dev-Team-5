using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class StageCarouselView : MonoBehaviour {
    [SerializeField, Min(.01f)] private float duration = .2f;
    private StageSelection selection;
    private StageProgressionState progression;
    private RectTransform[] cards;
    private Text[] labels;
    private Button[] navigationButtons;
    private GameCatalog catalog;
    private float visualIndex, fromIndex, elapsed;
    public float VisualIndex => visualIndex;
    public bool IsSettled => Mathf.Abs(visualIndex-selection.SelectedStageIndex) < .001f;
    public void Initialize(GameCatalog data, StageSelection selected, StageProgressionState progress) {
        catalog = data; selection = selected; progression = progress;
        cards = new RectTransform[data.Stages.Count]; labels = new Text[cards.Length];
        navigationButtons = new Button[cards.Length];
        gameObject.AddComponent<RectMask2D>();
        for (int i=0;i<cards.Length;i++) {
            cards[i] = UiFactory.Rect("Stage Card "+i,transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,225));
            cards[i].gameObject.AddComponent<Image>().color = new Color(.16f,.24f,.32f);
            int index = i;
            navigationButtons[i] = cards[i].gameObject.AddComponent<Button>();
            navigationButtons[i].targetGraphic = cards[i].GetComponent<Image>();
            navigationButtons[i].onClick.AddListener(()=>NavigateCard(index));
            labels[i] = UiFactory.Label(cards[i],"",new Vector2(.5f,.5f),Vector2.zero,new Vector2(260,200),27);
        }
        visualIndex = fromIndex = selection.SelectedStageIndex; elapsed = duration;
        selection.Changed += Retarget; progression.Changed += Render;
        Render();
    }
    private void NavigateCard(int index) {
        // During interpolation the visual center is in flight. Wait for it before accepting a mouse step.
        if (!IsSettled) return;
        if (index == selection.SelectedStageIndex-1) selection.MovePrevious();
        else if (index == selection.SelectedStageIndex+1) selection.MoveNext();
    }
    private void Retarget() { fromIndex = visualIndex; elapsed = 0; Render(); }
    private void Update() { Advance(Time.unscaledDeltaTime); }
    public void Advance(float delta) {
        if (cards == null) return;
        elapsed = Mathf.Min(duration,elapsed + delta);
        float t = Mathf.SmoothStep(0,1,elapsed/duration);
        visualIndex = Mathf.Lerp(fromIndex,selection.SelectedStageIndex,t); Render();
    }
    private void Render() {
        for (int i=0;i<cards.Length;i++) {
            float distance = i-visualIndex;
            cards[i].anchoredPosition = new Vector2(distance*325,15*(1-Mathf.Clamp01(Mathf.Abs(distance))));
            cards[i].localScale = Vector3.one*Mathf.Lerp(.78f,1,1-Mathf.Clamp01(Mathf.Abs(distance)));
            cards[i].gameObject.SetActive(Mathf.Abs(distance) < 1.9f);
            navigationButtons[i].interactable = IsSettled && Mathf.Abs(i-selection.SelectedStageIndex)==1;
            labels[i].text = catalog.Stages[i].DisplayName + "\n" + (progression.IsUnlocked(i) ? "Unlocked" : "Locked")
                + (i == selection.SelectedStageIndex ? "\nSelected Stage" : "");
        }
    }
    private void OnDestroy() {
        if (selection != null) selection.Changed -= Retarget;
        if (progression != null) progression.Changed -= Render;
    }
}
}
