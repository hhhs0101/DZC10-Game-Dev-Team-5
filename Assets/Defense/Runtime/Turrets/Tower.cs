using UnityEngine;
namespace Defense {
public class Tower : MonoBehaviour, IDamageable {
    private TurretDefinition definition;
    private EnemyRegistry registry;
    private TowerPlacementSlot placedSlot;
    private TowerLifetime lifetime;
    public float CurrentHealth { get; private set; }
    public float MaximumHealth => definition != null ? definition.MaximumHealth : 0;
    public bool IsDisrupted => AttackDisabled;
    public TowerType TowerType => definition != null ? definition.TowerType : TowerType.Basic;
    public TurretDefinition Definition => definition;
    public AttackDisableStatus AttackStatus { get; } = new AttackDisableStatus();
    public bool AttackDisabled => AttackStatus.IsDisabled;
    public float AttackRange => definition != null ? definition.AttackRange : 0;
    internal EnemyRegistry CombatRegistry => registry;
    public event System.Action<float,float> HealthChanged;
    public event System.Action<TurretDefinition> Initialized;
    // Both damage attacks and support pulses drive the existing presentation observer.
    public event System.Action<Enemy> Attacked;

    public void ReceiveDamage(float amount) {
        if(!isActiveAndEnabled || CurrentHealth <= 0 || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        CurrentHealth = Mathf.Max(0,CurrentHealth-amount); HealthChanged?.Invoke(CurrentHealth,MaximumHealth);
        if(CurrentHealth > 0) return;
        if(placedSlot != null) placedSlot.Release(this);
        gameObject.SetActive(false); Destroy(gameObject);
    }
    public void StartLifetime(TowerPlacementSlot slot, GameFlow flow, float energyCost) {
        placedSlot = slot;
        lifetime = gameObject.AddComponent<TowerLifetime>();
        lifetime.Initialize(this, slot, flow, energyCost);
    }
    public void Initialize(TurretDefinition data, EnemyRegistry enemies) {
        definition = data; registry = enemies; AttackStatus.Clear();
        CurrentHealth = data.MaximumHealth;
        OnInitialized();
        Initialized?.Invoke(data); HealthChanged?.Invoke(CurrentHealth,MaximumHealth);
    }
    protected virtual void OnInitialized() { }
    protected bool CanAct {
        get {
            if(!isActiveAndEnabled || CurrentHealth <= 0 || AttackDisabled || definition == null || registry == null) return false;
            var flow = registry.GetComponent<GameFlow>();
            return flow == null || flow.IsPlaying;
        }
    }
    protected virtual void Update() {
        if (definition == null || CurrentHealth <= 0 || Time.deltaTime <= 0) return;
        var flow = registry.GetComponent<GameFlow>(); if(flow != null && !flow.IsPlaying) return;
        if (lifetime != null && !lifetime.Advance(Time.deltaTime)) return;
        AttackStatus.Advance(Time.deltaTime);
        AdvanceBehavior(Time.deltaTime);
    }
    protected virtual void AdvanceBehavior(float deltaTime) { }
    protected void NotifyAttack(Enemy target) => Attacked?.Invoke(target);
    protected virtual void OnDrawGizmosSelected() {
        if (definition == null || registry == null) return;
        Gizmos.color = Color.cyan;
        for (int i=0;i<48;i++) {
            float a=i*Mathf.PI*2/48, b=(i+1)*Mathf.PI*2/48;
            Gizmos.DrawLine(transform.position+PlanarSpace.World(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),0,registry.Plane)*AttackRange,
                transform.position+PlanarSpace.World(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),0,registry.Plane)*AttackRange);
        }
    }
}
}
