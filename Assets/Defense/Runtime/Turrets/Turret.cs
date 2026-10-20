using UnityEngine;
namespace Defense {
public abstract class Turret : MonoBehaviour {
    [SerializeField, Tooltip("Attack implementation on this prefab.")] private AttackBehaviour attack;
    private TurretDefinition definition;
    private EnemyRegistry registry;
    private ITargetingStrategy targeting;
    private float cooldown;
    public TurretDefinition Definition => definition;
    // Shared runtime source for target eligibility, gizmos and read-only inspection.
    public float EffectiveAttackRange => definition != null ? definition.AttackRange : 0;
    public event System.Action<TurretDefinition> Initialized;
    public void Initialize(TurretDefinition data, EnemyRegistry enemies) {
        definition = data; registry = enemies; targeting = data.Targeting; cooldown = 0;
        Initialized?.Invoke(data);
    }
    private void Update() {
        if (definition == null || Time.deltaTime <= 0) return;
        cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
        if (cooldown > 0) return;
        // Re-evaluate every shot so Closest remains physical nearest, even if targets cross.
        Enemy target = targeting.Select(transform.position, EffectiveAttackRange, registry.Enemies,registry.Plane);
        if (target == null) return;
        attack.Attack(target, definition.Damage);
        cooldown = definition.AttackCooldown;
    }
    protected virtual void OnDrawGizmosSelected() {
        if (definition == null) return;
        Gizmos.color = Color.cyan;
        for (int i=0;i<48;i++) {
            float a=i*Mathf.PI*2/48, b=(i+1)*Mathf.PI*2/48;
            Gizmos.DrawLine(transform.position+PlanarSpace.World(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),0,registry.Plane)*EffectiveAttackRange,
                transform.position+PlanarSpace.World(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),0,registry.Plane)*EffectiveAttackRange);
        }
    }
}
}
