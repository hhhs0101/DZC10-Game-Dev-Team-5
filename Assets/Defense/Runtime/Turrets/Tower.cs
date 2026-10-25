using UnityEngine;
namespace Defense {
public abstract class Tower : MonoBehaviour, IDamageable {
    [SerializeField, Tooltip("Attack implementation on this prefab.")] private AttackBehaviour attack;
    private TurretDefinition definition;
    private EnemyRegistry registry;
    private ITargetingStrategy targeting;
    private float cooldown, cleanseClock;
    private TowerPlacementSlot placedSlot;
    private readonly object burnToken = new object();
    public float CurrentHealth { get; private set; }
    public float MaximumHealth => definition != null ? definition.MaximumHealth : 0;
    public bool IsDisrupted => AttackDisabled;
    public TowerType TowerType => definition != null ? definition.TowerType : TowerType.Basic;
    public event System.Action<float,float> HealthChanged;
    public void ReceiveDamage(float amount) {
        if(!isActiveAndEnabled || CurrentHealth <= 0 || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        CurrentHealth = Mathf.Max(0,CurrentHealth-amount); HealthChanged?.Invoke(CurrentHealth,MaximumHealth);
        if(CurrentHealth > 0) return;
        if(placedSlot != null) placedSlot.Release(this);
        gameObject.SetActive(false); Destroy(gameObject);
    }
    private TowerLifetime lifetime;
    public void StartLifetime(TowerPlacementSlot slot, GameFlow flow, float energyCost) {
        placedSlot = slot;
        lifetime = gameObject.AddComponent<TowerLifetime>();
        lifetime.Initialize(this, slot, flow, energyCost);
    }
    public TurretDefinition Definition => definition;
    public AttackDisableStatus AttackStatus { get; } = new AttackDisableStatus();
    public bool AttackDisabled => AttackStatus.IsDisabled;
    // Shared runtime source for target eligibility, gizmos and read-only inspection.
    public float EffectiveAttackRange => definition != null ? definition.AttackRange : 0;
    public event System.Action<TurretDefinition> Initialized;
    public event System.Action<Enemy> Attacked;
    public void Initialize(TurretDefinition data, EnemyRegistry enemies) {
        definition = data; registry = enemies; targeting = data.Targeting; cooldown = 0; AttackStatus.Clear();
        CurrentHealth = data.MaximumHealth; cleanseClock = 0;
        Initialized?.Invoke(data); HealthChanged?.Invoke(CurrentHealth,MaximumHealth);
    }
    protected virtual void Update() {
        if (definition == null || CurrentHealth <= 0 || Time.deltaTime <= 0) return;
        var flow = registry.GetComponent<GameFlow>(); if(flow != null && !flow.IsPlaying) return;
        if (lifetime != null && !lifetime.Advance(Time.deltaTime)) return;
        AttackStatus.Advance(Time.deltaTime);
        cooldown = Mathf.Max(0, cooldown - Time.deltaTime);
        if (AttackDisabled) return;
        if (IsSupportTower) { AdvanceSupport(Time.deltaTime); return; }
        if (cooldown > 0) return;
        // Re-evaluate every shot so Closest remains physical nearest, even if targets cross.
        Enemy target = FindTarget();
        if (target == null) return;
        Attack(target);
        cooldown = definition.AttackCooldown;
    }
    public Enemy FindTarget() => targeting != null && registry != null ? targeting.Select(transform.position,EffectiveAttackRange,registry.Enemies,registry.Plane) : null;
    public float EffectiveDamage {
        get {
            if(definition == null) return 0;
            float multiplier = 1;
            foreach(var slot in Object.FindObjectsByType<TowerPlacementSlot>()) {
                var source = slot.Occupant;
                if(source == null || source == this || !source.isActiveAndEnabled || source.Definition == null || source.AttackDisabled || source.TowerType != TowerType.Mayonnaise || source.gameObject.scene != gameObject.scene) continue;
                if(PlanarSpace.SqrDistance(source.transform.position,transform.position,registry.Plane) <= source.EffectiveAttackRange * source.EffectiveAttackRange)
                    multiplier = Mathf.Max(multiplier,source.Definition.DamageBoost);
            }
            return definition.Damage * multiplier;
        }
    }
    public void Attack(Enemy target) {
        if(!isActiveAndEnabled || CurrentHealth <= 0 || AttackDisabled || target == null || !target.IsAlive || definition == null) return;
        var flow = registry.GetComponent<GameFlow>(); if(flow != null && !flow.IsPlaying) return;
        float damage = EffectiveDamage;
        PerformAttack(target,damage);
        Attacked?.Invoke(target);
    }
    // The public entry points preserve all original guards, timing and event ordering.
    protected virtual bool IsSupportTower => false;
    protected abstract void PerformAttack(Enemy target, float damage);
    internal EnemyRegistry CombatRegistry => registry;
    internal AttackBehaviour AttackComponent => attack;
    internal object StatusToken => burnToken;
    internal void NotifySupportAttack() => Attacked?.Invoke(null);
    public void AdvanceSupport(float deltaTime) => SupportTower.Advance(this,deltaTime,ref cleanseClock);
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
