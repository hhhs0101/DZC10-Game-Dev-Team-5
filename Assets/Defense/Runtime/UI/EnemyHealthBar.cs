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
    [SerializeField] private int sortingOrder = 20;
    private EnemyHealth health;
    private GameObject bar;
    private RectTransform[] fills = new RectTransform[0];
    private Camera viewCamera;
    private SpriteRenderer appearance;
    private static readonly Vector2[] CornPositions = {
        new Vector2(.30f,.78f), new Vector2(.70f,.64f), new Vector2(.32f,.48f),
        new Vector2(.69f,.32f), new Vector2(.34f,.16f)
    };
    public float NormalizedHealth { get; private set; }
    public bool IsVisible => bar != null && bar.activeInHierarchy;
    private void Awake() {
        health = GetComponent<EnemyHealth>();
        appearance = GetComponentInChildren<SpriteRenderer>();
        bar = new GameObject("Enemy HP",typeof(RectTransform),typeof(Canvas)); bar.transform.SetParent(transform,false);
        bar.transform.localPosition = offset;
        var canvas = bar.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.sortingOrder = sortingOrder;
        var rect = bar.GetComponent<RectTransform>(); rect.sizeDelta = size*100; rect.localScale = Vector3.one*.01f;

    }
    private static RectTransform CreateImage(string name,Transform parent,Color color) {
        var go = new GameObject(name,typeof(RectTransform),typeof(Image)); var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return rect;
    }
    private void LateUpdate() {
        if(viewCamera == null) viewCamera = Camera.main;
        if(viewCamera != null) bar.transform.rotation = viewCamera.transform.rotation;
        if (health.LayerCount > 1 && appearance != null && appearance.sprite != null) {
            // Keep each miniature bar over a different part of the visible corn artwork.
            var bounds = appearance.bounds;
            bar.transform.position = bounds.center;
            var scale = bar.transform.lossyScale;
            float sx = Mathf.Max(.0001f,Mathf.Abs(scale.x));
            float sy = Mathf.Max(.0001f,Mathf.Abs(scale.y));
            for(int i=0;i<fills.Length;i++) {
                var row = (RectTransform)fills[i].parent;
                var point = CornPositions[i % CornPositions.Length];
                row.anchorMin = row.anchorMax = new Vector2(.5f,.5f);
                row.pivot = new Vector2(.5f,.5f);
                row.anchoredPosition = new Vector2((point.x-.5f)*bounds.size.x/sx,(point.y-.5f)*bounds.size.y/sy);
                row.sizeDelta = new Vector2(bounds.size.x*.30f/sx,Mathf.Max(.035f,bounds.size.y*.045f)/sy);
            }
        } else bar.transform.localPosition = offset;
    }
    private void OnEnable() { health.Changed += Refresh; Refresh(health.Current,health.Maximum); }
    private void OnDisable() { health.Changed -= Refresh; if(bar != null) bar.SetActive(false); }
    private void Refresh(float current,float maximum) {
        NormalizedHealth = maximum > 0 ? Mathf.Clamp01(current/maximum) : 0;
        int count = health.LayerCount;
        if (fills.Length != count) {
            foreach(Transform child in bar.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            fills = new RectTransform[count];
            var root = bar.GetComponent<RectTransform>();
            float rowHeight = size.y*100;
            root.sizeDelta = new Vector2(size.x*100,count*rowHeight+(count-1)*3);
            for(int i=0;i<count;i++) {
                var row = CreateImage("Health Layer "+(i+1),root,backgroundColor);
                row.anchorMin = row.anchorMax = new Vector2(.5f,1);
                row.pivot = new Vector2(.5f,1);
                row.sizeDelta = new Vector2(size.x*100,rowHeight);
                row.anchoredPosition = new Vector2(0,-i*(rowHeight+3));
                fills[i] = CreateImage("Fill",row,fillColor);
            }
        }
        bar.SetActive(isActiveAndEnabled && NormalizedHealth > 0);
        // Every bar displays its own target health, including simultaneous splash damage.
        for(int i=0;i<count;i++) {
            float layerHealth = health.GetLayerHealth(i);
            float fraction = health.LayerMaximum > 0 ? Mathf.Clamp01(layerHealth/health.LayerMaximum) : 0;
            fills[i].anchorMax = new Vector2(fraction,1);
        }
    }
}
}
