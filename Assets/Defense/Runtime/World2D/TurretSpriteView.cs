using UnityEngine;
namespace Defense {
// Optional observer on the shared 2D prefab. Only the visual child changes.
[DisallowMultipleComponent, RequireComponent(typeof(Turret))]
public sealed class TurretSpriteView : MonoBehaviour {
    [SerializeField] private SpriteRenderer appearance;
    private Turret turret;
    private Sprite originalSprite;
    private Color originalColor;
    private Vector3 originalScale, originalPosition;
    private void Awake() {
        turret = GetComponent<Turret>();
        originalSprite = appearance.sprite; originalColor = appearance.color;
        originalScale = appearance.transform.localScale; originalPosition = appearance.transform.localPosition;
    }
    private void OnEnable() { turret.Initialized += Refresh; Refresh(turret.Definition); }
    private void Refresh(TurretDefinition definition) {
        bool custom = definition != null && definition.Sprite != null;
        appearance.sprite = custom ? definition.Sprite : originalSprite;
        appearance.color = custom ? Color.white : originalColor;
        appearance.transform.localScale = custom ? new Vector3(definition.SpriteScale,definition.SpriteScale,1) : originalScale;
        appearance.transform.localPosition = custom ? new Vector3(definition.SpriteOffset.x,definition.SpriteOffset.y,originalPosition.z) : originalPosition;
    }
    private void OnDisable() { if (turret != null) turret.Initialized -= Refresh; }
}
}
