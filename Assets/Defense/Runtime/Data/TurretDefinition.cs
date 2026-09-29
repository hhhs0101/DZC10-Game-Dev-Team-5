using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Turret Definition")]
public sealed class TurretDefinition : ScriptableObject {
    [SerializeField] private string displayName = "Basic Turret";
    [SerializeField] private Turret prefab;
    [SerializeField, Min(0)] private int cost = 50;
    [SerializeField, Min(0.01f)] private float damage = 10;
    [SerializeField, Min(0.01f)] private float attackRange = 3;
    [SerializeField, Min(0.01f)] private float attackCooldown = 0.75f;
    [SerializeField] private TargetingStrategy targeting;
    public string DisplayName => displayName;
    public Turret Prefab => prefab;
    public int Cost => cost;
    public float Damage => damage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public TargetingStrategy Targeting => targeting;
}
}
