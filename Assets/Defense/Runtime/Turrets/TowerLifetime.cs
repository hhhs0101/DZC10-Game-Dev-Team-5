using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Created only after successful placement. Preview and sandbox actors stay untimed.
public sealed class TowerLifetime : MonoBehaviour {
    public const float RechargeCycles = 4f;
    public const float MinimumLifetime = 20f;
    // Preserve existing tower durations when passive Energy regeneration is rebalanced.
    public const float LifetimeReferenceRegeneration = .36f;
    private Turret tower;
    private TowerPlacementSlot slot;
    private GameFlow flow;
    private RectTransform display;
    private Text label;
    private Image fill, healthFill;
    private bool expired;
    public float Duration { get; private set; }
    public float Remaining { get; private set; }
    public static float DurationForCost(float energyCost) {
        if (float.IsNaN(energyCost) || float.IsInfinity(energyCost) || energyCost < 0)
            energyCost = 0;
        return Mathf.Max(MinimumLifetime, RechargeCycles * energyCost / LifetimeReferenceRegeneration);
    }
    public void Initialize(Turret owner, TowerPlacementSlot placementSlot, GameFlow gameFlow, float energyCost) {
        tower = owner; slot = placementSlot; flow = gameFlow;
        Duration = Remaining = DurationForCost(energyCost);
        display = UiFactory.Rect("Tower countdown", transform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(100,32));
        var canvas = display.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 100;
        // No GraphicRaycaster: the display must never block card or map input.
        var background = UiFactory.Panel("Background", display, new Color(.05f,.08f,.1f,.85f));
        background.GetComponent<Image>().raycastTarget = false;
        var bar = UiFactory.Rect("Remaining", display, new Vector2(0,0), new Vector2(0,2), new Vector2(100,4));
        bar.pivot = new Vector2(0,0);
        fill = bar.gameObject.AddComponent<Image>(); fill.raycastTarget = false;
        label = UiFactory.Label(display, "", new Vector2(.5f,.5f), new Vector2(0,2), new Vector2(100,28), 22);
        var hp = UiFactory.Rect("Tower health",display,new Vector2(0,0),new Vector2(0,-4),new Vector2(100,3));
        hp.pivot = new Vector2(0,0); healthFill = hp.gameObject.AddComponent<Image>();
        healthFill.raycastTarget = false; healthFill.color = new Color(.3f,.85f,.3f);
        RefreshDisplay();
    }
    // Driven before the turret's attack so an expired tower cannot fire one extra shot.
    public bool Advance(float deltaTime) {
        if (expired) return false;
        if (flow == null || !flow.IsPlaying) return false;
        if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return true;
        Remaining = Mathf.Max(0, Remaining - deltaTime);
        if (Remaining > 0) return true;
        expired = true;
        if (slot != null) slot.Release(tower);
        gameObject.SetActive(false);
        Destroy(gameObject);
        return false;
    }
    private void LateUpdate() { if (display != null) RefreshDisplay(); }
    private void RefreshDisplay() {
        // Keep the UI above the artwork, independent of nonuniform slot/tower transforms.
        var position = tower.transform.position + Vector3.up * 1.6f;
        foreach (var sprite in tower.GetComponentsInChildren<SpriteRenderer>()) {
            if (sprite.enabled) position.y = Mathf.Max(position.y, sprite.bounds.max.y + .25f);
        }
        display.position = position; display.rotation = Quaternion.identity;
        display.localScale = Vector3.one;
        var scale = display.lossyScale;
        display.localScale = new Vector3(.012f / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
            .012f / Mathf.Max(.0001f, Mathf.Abs(scale.y)), .012f / Mathf.Max(.0001f, Mathf.Abs(scale.z)));
        label.text = Mathf.CeilToInt(Remaining) + "s";
        healthFill.rectTransform.sizeDelta = new Vector2(100 * (tower.MaximumHealth > 0 ? tower.CurrentHealth / tower.MaximumHealth : 0),3);
        var color = Remaining <= 5 ? new Color(1,.45f,.25f) : new Color(.4f,.95f,.75f);
        label.color = color; fill.color = color;
        fill.rectTransform.sizeDelta = new Vector2(100 * Remaining / Duration, 4);
    }
    private void OnDestroy() { if (slot != null) slot.Release(tower); }
}
}
