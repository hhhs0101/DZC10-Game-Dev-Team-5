using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Enemy Definition")]
public sealed class EnemyDefinition : ScriptableObject {
    [SerializeField] private Enemy prefab;
    [SerializeField, Min(1)] private float health = 40;
    [SerializeField, Min(0.01f)] private float movementSpeed = 1.5f;
    [SerializeField, Min(1)] private int baseDamage = 1;
    [SerializeField, Min(0), Tooltip("Resources awarded once when this enemy is killed. Reaching the Base grants nothing.")]
    private int resourceReward = 10;
    public int ResourceReward => Mathf.Max(0, resourceReward);
    public Enemy Prefab => prefab;
    public float Health => health;
    public float MovementSpeed => movementSpeed;
    public int BaseDamage => baseDamage;
}
}
