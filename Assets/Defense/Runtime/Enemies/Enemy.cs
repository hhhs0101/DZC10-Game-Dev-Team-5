using System;
using UnityEngine;
namespace Defense {
[RequireComponent(typeof(EnemyHealth), typeof(EnemyPathFollower))]
public abstract class Enemy : MonoBehaviour, IDamageable {
    private EnemyHealth health;
    private EnemyPathFollower movement;
    private EnemyRegistry registry;
    private IDamageable baseTarget;
    private int baseDamage;
    private bool active;
    public bool IsAlive => active && isActiveAndEnabled && health.Current > 0;
    public float CurrentHealth => health != null ? health.Current : 0;
    public float MaximumHealth => health != null ? health.Maximum : 0;
    public int ResourceReward { get; private set; }
    public event Action<Enemy> Died;
    public void Initialize(EnemyDefinition data, WaypointPath path, IDamageable target, EnemyRegistry enemies) {
        health = GetComponent<EnemyHealth>(); movement = GetComponent<EnemyPathFollower>();
        registry = enemies; baseTarget = target; baseDamage = data.BaseDamage;
        ResourceReward = data.ResourceReward;
        health.Initialize(data.Health);
        health.Depleted += Die; movement.ReachedEnd += ReachBase;
        active = true; registry.Register(this);
        movement.Initialize(path, data.MovementSpeed);
    }
    public void ReceiveDamage(float amount) { if (IsAlive) health.ReceiveDamage(amount); }
    protected virtual void Die() {
        if (!active) return;
        Retire(); Died?.Invoke(this); Destroy(gameObject);
    }
    private void ReachBase() {
        if (!active) return;
        Retire(); baseTarget.ReceiveDamage(baseDamage); Destroy(gameObject);
    }
    private void Retire() {
        active = false; movement.Stop();
        if (registry != null) registry.Unregister(this);
    }
    protected virtual void OnDisable() { if (active) Retire(); }
    protected virtual void OnDestroy() {
        if (health != null) health.Depleted -= Die;
        if (movement != null) movement.ReachedEnd -= ReachBase;
        if (registry != null) registry.Unregister(this);
    }
}
}
