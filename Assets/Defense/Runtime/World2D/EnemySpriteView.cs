using UnityEngine;
namespace Defense {
// Optional presentation observer. Combat, movement and collision remain on the unchanged root.
[DisallowMultipleComponent, RequireComponent(typeof(Enemy))]
public sealed class EnemySpriteView : MonoBehaviour {
    [SerializeField] private SpriteRenderer appearance;
    private Enemy enemy;
    private Sprite originalSprite;
    private Color originalColor;
    private void Awake() {
        enemy=GetComponent<Enemy>();
        originalSprite=appearance.sprite; originalColor=appearance.color;
    }
    private void OnEnable() { enemy.Initialized+=Refresh; Refresh(enemy.Definition); }
    private void Refresh(EnemyDefinition definition) {
        bool custom=definition!=null && definition.Sprite!=null;
        appearance.sprite=custom ? definition.Sprite : originalSprite;
        appearance.color=custom ? Color.white : originalColor;
    }
    private void OnDisable() { if(enemy!=null) enemy.Initialized-=Refresh; }
}
}
