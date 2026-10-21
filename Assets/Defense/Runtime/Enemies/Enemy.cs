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
    private EnemyRoleAbility roleAbility;
    public bool IsAlive => active && isActiveAndEnabled && health.Current > 0;
    public float CurrentHealth => health != null ? health.Current : 0;
    public float MaximumHealth => health != null ? health.Maximum : 0;
    public EnemyDefinition Definition { get; private set; }
    public int ResourceReward { get; private set; }
    public EnemyPathFollower Movement => movement;
    // Observers can bind presentation without introducing visual concerns into enemy behavior.
    public event Action<EnemyDefinition> Initialized;
    public event Action<Enemy> Died;
    public void Initialize(EnemyDefinition data, WaypointPath path, IDamageable target, EnemyRegistry enemies) {
        if(active) Retire();
        if(health != null) health.Depleted -= Die;
        if(movement != null) { movement.ReachedEnd -= ReachBase; movement.Moved -= OnMoved; }
        health = GetComponent<EnemyHealth>(); movement = GetComponent<EnemyPathFollower>();
        registry = enemies; baseTarget = target; baseDamage = data.BaseDamage;
        Definition = data; ResourceReward = data.ResourceReward;
        health.Initialize(data.Health);
        health.Depleted += Die; movement.ReachedEnd += ReachBase;
        movement.Initialize(path, data.MovementSpeed);
        movement.Moved += OnMoved;
        active = true; Initialized?.Invoke(data); registry.Register(this);
        roleAbility = EnemyRoleAbility.Configure(this,data,registry,roleAbility);
        if(!data.HasConfiguredBaseDamage) Debug.LogWarning(data.DisplayName+": endpoint damage is not configured (design says 'tower health'); Base arrival retires safely without damage or a kill reward.",this);
    }
    public void ReceiveDamage(float amount) { if (IsAlive) health.ReceiveDamage(registry.ResolveDamage(this,amount)); }
    protected virtual void Die() {
        if (!active) return;
        Retire(); Died?.Invoke(this); Destroy(gameObject);
    }
    private void ReachBase() {
        if (!active) return;
        Retire(); if(Definition.HasConfiguredBaseDamage) baseTarget.ReceiveDamage(baseDamage); Destroy(gameObject);
    }
    private void OnMoved() { if (active) registry.NotifyMoved(this); }
    private void Retire() {
        active = false; if(roleAbility != null) roleAbility.Shutdown(); movement.Stop(); movement.Moved -= OnMoved; movement.Modifiers.Clear();
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
