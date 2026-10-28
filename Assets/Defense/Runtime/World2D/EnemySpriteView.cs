using UnityEngine;
namespace Defense {
// Optional presentation observer. Combat, movement and collision remain on the unchanged root.
[DisallowMultipleComponent, RequireComponent(typeof(Enemy))]
public sealed class EnemySpriteView : MonoBehaviour {
    [SerializeField] private SpriteRenderer appearance;
    private Enemy enemy;
    private Sprite originalSprite;
    private Color originalColor;
    private Vector3 originalScale;
    private void Awake() {
        enemy=GetComponent<Enemy>();
        originalSprite=appearance.sprite; originalColor=appearance.color; originalScale=appearance.transform.localScale;
    }
    private void OnEnable() {
        enemy.Initialized += Refresh;
        enemy.Died += OnDied;
        Refresh(enemy.Definition);
    }
    private void Refresh(EnemyDefinition definition) {
        bool custom = definition != null && definition.Sprite != null;
        appearance.sprite = custom ? definition.Sprite : originalSprite;
        appearance.transform.localScale = originalScale * (definition != null ? definition.SpriteScale : 1);
        var color = custom ? Color.white : originalColor;
        if (enemy != null && enemy.IsDead) color.a = Enemy.DeadSpriteAlpha;
        appearance.color = color;
    }
    private void OnDied(Enemy _) {
        if (appearance == null) return;
        var color = appearance.color;
        color.a = Enemy.DeadSpriteAlpha;
        appearance.color = color;
    }
    private void OnDisable() {
        if (enemy != null) {
            enemy.Initialized -= Refresh;
            enemy.Died -= OnDied;
        }
    }
}
}
