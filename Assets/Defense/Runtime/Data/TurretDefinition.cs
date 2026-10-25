using UnityEngine;
namespace Defense {
public enum TowerType { Basic, Salt, BlackPepper, Ketchup, Mustard, Mayonnaise, HotSauce, SoySauce, Oil }
[CreateAssetMenu(menuName="Defense/Tower Definition")]
public sealed class TurretDefinition : ScriptableObject {
    [SerializeField] private string displayName = "Basic Tower";
    [SerializeField] private Tower prefab;
    [SerializeField, Min(0)] private int cost = 0;
    [SerializeField, Min(0.01f)] private float damage = 10;
    [SerializeField, Min(0.01f)] private float attackRange = 3;
    [SerializeField, Min(0.01f)] private float attackCooldown = 0.75f;
    [SerializeField] private TargetingStrategy targeting;
    [Header("2D presentation (optional)")]
    [SerializeField] private Sprite sprite;
    [SerializeField, Min(.01f)] private float spriteScale = 1;
    [SerializeField, Tooltip("Presentation-only offset from the unchanged placement point.")] private Vector2 spriteOffset;
    [Header("Combat role and editable balance")]
    [SerializeField] private TowerType towerType;
    [SerializeField, Min(1)] private float maximumHealth = 1000;
    [SerializeField, Min(0)] private float splashRadius = 1.5f;
    [SerializeField, Min(0)] private float burnDamagePerSecond = 5;
    [SerializeField, Min(.1f)] private float burnDuration = 4;
    [SerializeField, Range(0,1)] private float slowMultiplier = .5f;
    [SerializeField, Min(.1f)] private float slowDuration = 2;
    [SerializeField, Min(1)] private float damageBoost = 1.25f;
    [SerializeField, Min(.1f)] private float cleanseInterval = 5;
    public TowerType TowerType => towerType;
    public float MaximumHealth => Mathf.Max(1,maximumHealth);
    public float SplashRadius => splashRadius;
    public float BurnDamagePerSecond => burnDamagePerSecond;
    public float BurnDuration => burnDuration;
    public float SlowMultiplier => slowMultiplier;
    public float SlowDuration => slowDuration;
    public float DamageBoost => damageBoost;
    public float CleanseInterval => Mathf.Max(.1f,cleanseInterval);
    public Sprite Sprite => sprite;
    public float SpriteScale => spriteScale;
    public Vector2 SpriteOffset => spriteOffset;
    public string DisplayName => displayName;
    public Tower Prefab => prefab;
    public int Cost => cost;
    public float Damage => damage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public TargetingStrategy Targeting => targeting;
}
}
