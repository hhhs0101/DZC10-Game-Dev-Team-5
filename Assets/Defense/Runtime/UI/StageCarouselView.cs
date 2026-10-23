using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class StageCarouselView : MonoBehaviour {
    [SerializeField, Min(.01f)] private float duration = .2f;
    private StageSelection selection;
    private StageProgressionState progression;
    private RectTransform[] cards;
    private Text[] labels;
    private Image[] backgrounds;
    private MainMenuSkin skin;
    private Button[] navigationButtons;
    private GameCatalog catalog;
    private float visualIndex, fromIndex, elapsed;
    public float VisualIndex => visualIndex;
    public bool IsSettled => Mathf.Abs(visualIndex-selection.SelectedStageIndex) < .001f;
    public void Initialize(GameCatalog data, StageSelection selected, StageProgressionState progress, MainMenuSkin artwork = null) {
        catalog = data; selection = selected; progression = progress; skin = artwork;
        cards = new RectTransform[data.Stages.Count]; labels = new Text[cards.Length];
        navigationButtons = new Button[cards.Length]; backgrounds = new Image[cards.Length];
        gameObject.AddComponent<RectMask2D>();
        for (int i=0;i<cards.Length;i++) {
            cards[i] = UiFactory.Rect("Stage Card "+i,transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(270,225));
            backgrounds[i] = cards[i].gameObject.AddComponent<Image>();
            backgrounds[i].color = new Color(.16f,.24f,.32f);
            int index = i;
            navigationButtons[i] = cards[i].gameObject.AddComponent<Button>();
            navigationButtons[i].targetGraphic = cards[i].GetComponent<Image>();
            navigationButtons[i].onClick.AddListener(()=>NavigateCard(index));
            bool hasBoard = skin != null && skin.GetStageBoard(i,true) != null && skin.GetStageBoard(i,false) != null;
            labels[i] = UiFactory.Label(cards[i],"",new Vector2(.5f,.5f),hasBoard ? new Vector2(0,-96) : Vector2.zero,hasBoard ? new Vector2(260,64) : new Vector2(260,200),hasBoard ? 16 : 27);
            if (hasBoard) {
                var shadow = labels[i].gameObject.AddComponent<UnityEngine.UI.Shadow>();
                shadow.effectColor = new Color(0,0,0,.8f); shadow.effectDistance = new Vector2(1,-1);
            }
        }
        visualIndex = fromIndex = selection.SelectedStageIndex; elapsed = duration;
        selection.Changed += Retarget; progression.Changed += Render;
        Render();
    }
    private void NavigateCard(int index) {
        // During interpolation the visual center is in flight. Wait for it before accepting a mouse step.
        if (!IsSettled) return;
        if (index == selection.SelectedStageIndex-1) { GameAudio.PlayButtonClick(); selection.MovePrevious(); }
        else if (index == selection.SelectedStageIndex+1) { GameAudio.PlayButtonClick(); selection.MoveNext(); }
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
            // Logical carousel focus owns board brightness; lock, hover and press never choose its column.
            var board = skin != null ? skin.GetStageBoard(i,i==selection.SelectedStageIndex) : null;
            backgrounds[i].sprite = board; backgrounds[i].preserveAspect = board != null;
            backgrounds[i].color = board != null ? Color.white : new Color(.16f,.24f,.32f);
            navigationButtons[i].transition = board != null ? Selectable.Transition.None : Selectable.Transition.ColorTint;
            if (board != null) backgrounds[i].canvasRenderer.SetColor(Color.white);
            navigationButtons[i].interactable = IsSettled && Mathf.Abs(i-selection.SelectedStageIndex)==1;
            labels[i].text = catalog.Stages[i].DisplayName + "\n" + (progression.IsUnlocked(i) ? "Unlocked" : PlayerSession.DevMode ? "Dev access" : "Locked")
                + (i == selection.SelectedStageIndex ? "\nSelected Stage" : "");
        }
    }
    private void OnDestroy() {
        if (selection != null) selection.Changed -= Retarget;
        if (progression != null) progression.Changed -= Render;
    }
}
}
