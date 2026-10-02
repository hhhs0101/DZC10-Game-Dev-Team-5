using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Optional world-space view. No Enemy/health logic depends on this component.
[RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyHealthBar : MonoBehaviour {
    [SerializeField] private Vector3 offset = new Vector3(0,1.05f,0);
    [SerializeField] private Vector2 size = new Vector2(.9f,.11f);
    [SerializeField] private Color fillColor = new Color(.25f,.9f,.3f);
    [SerializeField] private Color backgroundColor = new Color(.2f,.08f,.08f);
    private EnemyHealth health;
    private GameObject bar;
    private RectTransform fill;
    private Camera viewCamera;
    public float NormalizedHealth { get; private set; }
    public bool IsVisible => bar != null && bar.activeInHierarchy;
    private void Awake() {
        health = GetComponent<EnemyHealth>();
        bar = new GameObject("Enemy HP",typeof(RectTransform),typeof(Canvas)); bar.transform.SetParent(transform,false);
        bar.transform.localPosition = offset;
        var canvas = bar.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = 20;
        var rect = bar.GetComponent<RectTransform>(); rect.sizeDelta = size*100; rect.localScale = Vector3.one*.01f;
        CreateImage("Background",rect,backgroundColor);
        fill = CreateImage("Fill",rect,fillColor);
    }
    private static RectTransform CreateImage(string name,Transform parent,Color color) {
        var go = new GameObject(name,typeof(RectTransform),typeof(Image)); var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
    }
    private void LateUpdate() {
        if(viewCamera == null) viewCamera = Camera.main;
        if(viewCamera != null) bar.transform.rotation = viewCamera.transform.rotation;
    }
    private void OnEnable() { health.Changed += Refresh; Refresh(health.Current,health.Maximum); }
    private void OnDisable() { health.Changed -= Refresh; if(bar != null) bar.SetActive(false); }
    private void Refresh(float current,float maximum) {
        NormalizedHealth = maximum > 0 ? Mathf.Clamp01(current/maximum) : 0;
        bar.SetActive(isActiveAndEnabled && NormalizedHealth > 0); fill.anchorMax = new Vector2(NormalizedHealth,1);
    }
}
}
