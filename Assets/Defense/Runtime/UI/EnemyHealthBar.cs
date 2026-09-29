using UnityEngine;
namespace Defense {
// Reusable view: observes health without changing enemy combat or lifecycle logic.
[RequireComponent(typeof(EnemyHealth), typeof(SpriteRenderer))]
public sealed class EnemyHealthBar : MonoBehaviour {
    [SerializeField] private Vector2 offset = new Vector2(0,.8f);
    [SerializeField] private Vector2 size = new Vector2(1.4f,.14f);
    [SerializeField] private Color fillColor = new Color(.25f,.9f,.3f);
    [SerializeField] private Color backgroundColor = new Color(.2f,.08f,.08f);
    private EnemyHealth health;
    private GameObject bar;
    private Transform fill;
    public float NormalizedHealth { get; private set; }
    public bool IsVisible => bar != null && bar.activeInHierarchy;
    private void Awake() {
        health = GetComponent<EnemyHealth>();
        var source = GetComponent<SpriteRenderer>();
        bar = new GameObject("Enemy HP"); bar.transform.SetParent(transform,false);
        bar.transform.localPosition = offset;
        CreateSprite("Background",source,backgroundColor,0);
        fill = CreateSprite("Fill",source,fillColor,1).transform;
    }
    private SpriteRenderer CreateSprite(string name, SpriteRenderer source, Color color, int order) {
        var child = new GameObject(name); child.transform.SetParent(bar.transform,false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite; renderer.color = color;
        renderer.sortingLayerID = source.sortingLayerID; renderer.sortingOrder = source.sortingOrder + 10 + order;
        Vector2 bounds = source.sprite.bounds.size;
        child.transform.localScale = new Vector3(size.x/bounds.x,size.y/bounds.y,1);
        return renderer;
    }
    private void OnEnable() { health.Changed += Refresh; Refresh(health.Current,health.Maximum); }
    private void OnDisable() { health.Changed -= Refresh; }
    private void Refresh(float current, float maximum) {
        NormalizedHealth = maximum > 0 ? Mathf.Clamp01(current/maximum) : 0;
        bar.SetActive(NormalizedHealth > 0);
        float spriteWidth = fill.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
        Vector3 scale = fill.localScale; scale.x = size.x * NormalizedHealth / spriteWidth; fill.localScale = scale;
        fill.localPosition = new Vector3(-size.x*(1-NormalizedHealth)/2,0,0);
    }
}
}
