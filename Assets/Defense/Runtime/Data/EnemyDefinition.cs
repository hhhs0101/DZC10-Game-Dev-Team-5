using UnityEngine;
namespace Defense {
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
    [SerializeField, Min(1)] private int baseDamage = 1;
    [SerializeField, Min(0), Tooltip("Resources awarded once when this enemy is killed. Reaching the Base grants nothing.")]
    private int resourceReward = 10;
    [SerializeField, Range(0,1)] private float candyDropChance;
    [SerializeField, Min(0)] private int candyDropAmount = 1;
    public float CandyDropChance => float.IsNaN(candyDropChance) ? 0 : Mathf.Clamp01(candyDropChance);
    public int CandyDropAmount => Mathf.Max(0,candyDropAmount);
    public int ResourceReward => Mathf.Max(0, resourceReward);
    public Enemy Prefab => prefab;
    public float Health => health;
    public float MovementSpeed => movementSpeed;
    public int BaseDamage => baseDamage;
}
}
