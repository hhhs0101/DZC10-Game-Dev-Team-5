using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Freezes the battle just before a stage boss spawns and explains the vegetable's place in Korean food culture.
public sealed class BossInfoCardUI : MonoBehaviour {
    private FixedIntervalSpawner schedule;
    private GameFlow flow;
    private GameObject overlay;
    private Image bossImage;
    private Text heading, body;
    public bool IsOpen => overlay != null && overlay.activeSelf;

    public void Initialize(Transform root, FixedIntervalSpawner spawnSchedule, GameFlow gameFlow) {
        schedule = spawnSchedule; flow = gameFlow;
        Build(root);
        if (schedule != null) schedule.BossIncoming += Show;
    }

    private void Build(Transform root) {
        overlay = UiFactory.Panel("Boss Info Card",root,new Color(0,0,0,.72f));
        var box = UiFactory.Rect("Card",overlay.transform,Vector2.zero,Vector2.zero,Vector2.zero);
        Stretch(box,new Vector2(.12f,.06f),new Vector2(.88f,.94f));
        var boxImage = box.gameObject.AddComponent<Image>();
        boxImage.sprite = UiFactory.LoadSprite("UI/Text Info Box");
        boxImage.preserveAspect = true;
        if (boxImage.sprite == null) boxImage.color = new Color(.86f,.65f,.38f);
        var fit = box.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = boxImage.sprite != null ? boxImage.sprite.rect.width / boxImage.sprite.rect.height : 4f / 3f;

        var ink = new Color(.24f,.12f,.05f);
        var warning = UiFactory.Label(box,"BOSS INCOMING!",Vector2.zero,Vector2.zero,Vector2.zero,40);
        Stretch(warning.rectTransform,new Vector2(.12f,.8f),new Vector2(.88f,.89f));
        warning.color = new Color(.72f,.12f,.08f); warning.fontStyle = FontStyle.Bold;

        // Boss art on the left, its story on the right, like a field-guide entry.
        var art = UiFactory.Rect("Boss Art",box,Vector2.zero,Vector2.zero,Vector2.zero);
        Stretch(art,new Vector2(.1f,.26f),new Vector2(.38f,.78f));
        bossImage = art.gameObject.AddComponent<Image>();
        bossImage.preserveAspect = true; bossImage.raycastTarget = false;

        heading = UiFactory.Label(box,"",Vector2.zero,Vector2.zero,Vector2.zero,30);
        Stretch(heading.rectTransform,new Vector2(.4f,.69f),new Vector2(.9f,.79f));
        heading.color = ink; heading.fontStyle = FontStyle.Bold; heading.alignment = TextAnchor.MiddleLeft;
        heading.resizeTextForBestFit = true; heading.resizeTextMinSize = 16; heading.resizeTextMaxSize = 30;

        body = UiFactory.Label(box,"",Vector2.zero,Vector2.zero,Vector2.zero,20);
        Stretch(body.rectTransform,new Vector2(.4f,.24f),new Vector2(.9f,.68f));
        body.color = ink; body.alignment = TextAnchor.UpperLeft; body.lineSpacing = 1.05f;
        body.verticalOverflow = VerticalWrapMode.Truncate;
        body.resizeTextForBestFit = true; body.resizeTextMinSize = 11; body.resizeTextMaxSize = 20;

        var button = UiFactory.Button(box,"Season it!",Vector2.zero,Close,new Vector2(.5f,.15f),new Vector2(240,71));
        SkinButton(button,"UI/Boss/season_it_button");
        overlay.SetActive(false);
    }

    private static void SkinButton(Button button, string resourcePrefix) {
        var normal = Resources.Load<Sprite>(resourcePrefix + "_normal");
        var hover = Resources.Load<Sprite>(resourcePrefix + "_hover");
        var pressed = Resources.Load<Sprite>(resourcePrefix + "_pressed");
        if (normal == null) {
            button.image.color = new Color(.55f,.24f,.14f,.95f);
            return;
        }
        button.image.sprite = normal;
        button.image.color = Color.white;
        button.image.preserveAspect = true;
        button.transition = Selectable.Transition.SpriteSwap;
        button.spriteState = new SpriteState {
            highlightedSprite = hover != null ? hover : normal,
            selectedSprite = hover != null ? hover : normal,
            pressedSprite = pressed != null ? pressed : normal
        };
        foreach (var label in button.GetComponentsInChildren<Text>()) label.enabled = false;
    }

    private void Show(EnemyDefinition boss) {
        if (!CulturalFacts.TryGetBoss(boss.EnemyTypeId,out var title,out var text)) { title = boss.DisplayName; text = ""; }
        heading.text = title; body.text = text;
        bossImage.sprite = boss.Sprite; bossImage.enabled = boss.Sprite != null;
        overlay.SetActive(true); overlay.transform.SetAsLastSibling();
    }

    private void Update() {
        if (!IsOpen) return;
        // Hold the battle still while the card is read, even if something else resumes time.
        if (flow != null && flow.IsPlaying) Time.timeScale = 0;
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Close();
    }

    private void Close() {
        if (!IsOpen) return;
        overlay.SetActive(false);
        if (flow != null && flow.IsPlaying) Time.timeScale = 1;
        if (schedule != null) schedule.ResumeAfterBossAnnouncement();
    }

    private void OnDestroy() { if (schedule != null) schedule.BossIncoming -= Show; }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max) {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
}
