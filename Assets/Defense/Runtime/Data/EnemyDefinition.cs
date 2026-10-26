using UnityEngine;
namespace Defense {
// Existing content is Basic (zero); stable string enemy IDs remain unchanged.
public enum EnemyRole { Basic = 0, SpeedSupport = 1, ShieldSupport = 2, Disruptor = 3, Boss = 4, Split = 5 }
public enum VegetableType { Other, Carrot, Tomato, Eggplant, Garlic, BellPepper, Corn, Potato, Broccoli }
[CreateAssetMenu(menuName="Defense/Enemy Definition")]
public sealed class EnemyDefinition : ScriptableObject {
    [Header("Identity and presentation")]
    [SerializeField, Tooltip("Stable content ID for new enemy variants. Keep it unchanged when renaming artwork.")]
    private string enemyTypeId;
    [SerializeField] private string displayName;
    [SerializeField, Tooltip("Optional native 2D artwork. Empty preserves the prefab's original visual.")]
    private Sprite sprite;
    [SerializeField] private Enemy prefab;
    public string EnemyTypeId => enemyTypeId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
    public Sprite Sprite => sprite;
    [Header("Gameplay")]

    [SerializeField, Min(1)] private float health = 40;
    [SerializeField, Min(0.01f)] private float movementSpeed = 1.5f;
    [SerializeField, Min(0)] private int baseDamage = 1;
    [SerializeField, Tooltip("Explicit TBD endpoint behavior. Retires safely at Base without applying invented damage.")]
    private bool baseDamageUnconfigured;
    [Header("Role and optional abilities")]
    [SerializeField] private EnemyRole role;
    [SerializeField, Min(0), Tooltip("Support radius in XY world units. Zero means design tuning is not configured.")]
    private float supportArea;
    [SerializeField, Min(1), Tooltip("Garlic multiplier; strongest boost wins, then combines with strongest slow.")]
    private float speedIncrease = 1.5f;
    [SerializeField, Min(0), Tooltip("Fraction of incoming damage absorbed by the Bell Pepper aura, including itself.")]
    private float shieldValue = .6f;
    [SerializeField, Min(0), Tooltip("Potato XY reach. Zero means not configured.")]
    private float disruptionRange;
    [SerializeField, Min(0), Tooltip("Seconds between Potato attempts, including attempts with no Tower. Zero means not configured.")]
    private float disruptionCooldown;
    [SerializeField, Min(0), Tooltip("Attack-disable seconds; survives source death until expiry. Zero means not configured.")]
    private float disruptionDuration;
    [SerializeField] private VegetableType vegetableType;
    [SerializeField, Min(1)] private int splitCount = 5;
    [SerializeField, Min(1)] private int summonCount = 3;
    [SerializeField, Min(.1f)] private float summonInterval = 5;
    [SerializeField] private EnemyDefinition[] summonTypes = System.Array.Empty<EnemyDefinition>();
    [SerializeField, Min(.1f)] private float towerAttackRange = 3;
    [SerializeField, Min(.1f)] private float towerAttackInterval = 5;
    [SerializeField, Range(0,1)] private float towerDamageFraction = .66f;
    public VegetableType VegetableType => vegetableType;
    public int SplitCount => Mathf.Max(1,splitCount);
    public int SummonCount => Mathf.Max(1,summonCount);
    public float SummonInterval => Mathf.Max(.1f,summonInterval);
    public System.Collections.Generic.IReadOnlyList<EnemyDefinition> SummonTypes => summonTypes;
    public float TowerAttackRange => towerAttackRange;
    public float TowerAttackInterval => Mathf.Max(.1f,towerAttackInterval);
    public float TowerDamageFraction => Mathf.Clamp01(towerDamageFraction);
    public EnemyRole Role => role;
    public bool HasConfiguredBaseDamage => !baseDamageUnconfigured;
    public float SupportArea => supportArea;
    public float SpeedIncrease => speedIncrease;
    public float ShieldValue => shieldValue;
    public float DisruptionRange => disruptionRange;
    public float DisruptionCooldown => disruptionCooldown;
    public float DisruptionDuration => disruptionDuration;
    private static bool Positive(float value) => value > 0 && !float.IsInfinity(value) && !float.IsNaN(value);
    public bool AbilityConfigured => string.IsNullOrEmpty(AbilityConfigurationIssue);
    public string AbilityConfigurationIssue {
        get {
            switch (role) {
                case EnemyRole.SpeedSupport:
                    return Positive(supportArea) && Positive(speedIncrease) && speedIncrease >= 1 ? null : "Garlic supportArea (radius) or speed multiplier is not configured.";
                case EnemyRole.ShieldSupport:
                    return Positive(supportArea) && Positive(shieldValue) && shieldValue <= 1 ? null : "Bell Pepper support radius or shield fraction is invalid.";
                case EnemyRole.Disruptor:
                    return Positive(disruptionRange) && Positive(disruptionCooldown) && Positive(disruptionDuration) ? null : "Potato disruption range, cooldown and duration must all be configured.";
                default: return null;
            }
        }
    }
    [SerializeField, Min(0), Tooltip("Resources awarded once when this enemy is killed. Reaching the Base grants nothing.")]
    private int resourceReward = 10;
    [SerializeField, Range(0,1)] private float candyDropChance;
    [SerializeField, Min(0)] private int candyDropAmount = 1;
    public float CandyDropChance => float.IsNaN(candyDropChance) ? 0 : Mathf.Clamp01(candyDropChance);
    public int CandyDropAmount => Mathf.Max(0,candyDropAmount);
    public int ResourceReward => Mathf.Max(0, resourceReward);
    public Enemy Prefab => prefab;
    public float Health => health;
    // Apply the global pacing adjustment once, including summoned enemies.
    public float MovementSpeed => movementSpeed * 0.85f;
    public int BaseDamage => HasConfiguredBaseDamage ? baseDamage : 0;
}
}
