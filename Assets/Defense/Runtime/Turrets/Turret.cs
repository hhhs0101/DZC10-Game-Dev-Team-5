using UnityEngine;
namespace Defense {
public abstract class Turret : MonoBehaviour {
    [SerializeField, Tooltip("Attack implementation on this prefab.")] private AttackBehaviour attack;
    private TurretDefinition definition;
    private EnemyRegistry registry;
    private ITargetingStrategy targeting;
    private float cooldown;
    public void Initialize(TurretDefinition data, EnemyRegistry enemies) {
        definition = data; registry = enemies; targeting = data.Targeting; cooldown = 0;
    }
    private void Update() {
        if (definition == null || Time.deltaTime <= 0) return;
        cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
        if (cooldown > 0) return;
        // Re-evaluate every shot so Closest remains physical nearest, even if targets cross.
        Enemy target = targeting.Select(transform.position, definition.AttackRange, registry.Enemies);
        if (target == null) return;
        attack.Attack(target, definition.Damage);
        cooldown = definition.AttackCooldown;
    }
    protected virtual void OnDrawGizmosSelected() {
        if (definition == null) return;
        Gizmos.color = Color.cyan;
        for (int i=0;i<48;i++) {
            float a=i*Mathf.PI*2/48, b=(i+1)*Mathf.PI*2/48;
            Gizmos.DrawLine(transform.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*definition.AttackRange,
                transform.position+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*definition.AttackRange);
        }
    }
}
}
