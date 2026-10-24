using System;
using UnityEngine;
namespace Defense {
[RequireComponent(typeof(EnemyHealth), typeof(EnemyPathFollower))]
public abstract class Enemy : MonoBehaviour, IDamageable {
    public const float DeadSpriteAlpha = 0.4f;
    private EnemyHealth health;
    private EnemyPathFollower movement;
    private EnemyRegistry registry;
    private IDamageable baseTarget;
    private int baseDamage;
    private bool active;
    private EnemyRoleAbility roleAbility;
    public bool IsAlive => active && isActiveAndEnabled && health.Current > 0;
    public bool IsDead { get; private set; }
    public float CurrentHealth => health != null ? health.Current : 0;
    public float MaximumHealth => health != null ? health.Maximum : 0;
    public EnemySpawner Spawner { get; internal set; }
    public bool CanSplit { get; internal set; } = true;
    public EnemyRegistry Registry => registry;
    public Vector2 GetPosition() => transform.position;
    public void TakeDamage(int amount) => ReceiveDamage(amount);
    public EnemyDefinition Definition { get; private set; }
    public int ResourceReward { get; private set; }
    public EnemyPathFollower Movement => movement;
    // Observers can bind presentation without introducing visual concerns into enemy behavior.
    public event Action<EnemyDefinition> Initialized;
    public event Action<Enemy> Died;
    public void Initialize(EnemyDefinition data, WaypointPath path, IDamageable target, EnemyRegistry enemies, EnemyPathFollower continuation = null) {
        if(active) Retire();
        var effects = GetComponent<EnemyStatusEffects>(); if(effects != null) effects.Clear();
        if(health != null) health.Depleted -= Die;
        if(movement != null) { movement.ReachedEnd -= ReachBase; movement.Moved -= OnMoved; }
        health = GetComponent<EnemyHealth>(); movement = GetComponent<EnemyPathFollower>();
        registry = enemies; baseTarget = target; baseDamage = data.BaseDamage;
        Definition = data; ResourceReward = data.ResourceReward;
        health.Initialize(data.Health, data.VegetableType == VegetableType.Corn ? data.SplitCount : 1);
        health.Depleted += Die; movement.ReachedEnd += ReachBase;
        movement.Initialize(path, data.MovementSpeed);
        if(continuation != null) movement.ContinueFrom(continuation);
        movement.Moved += OnMoved;
        IsDead = false;
        active = true; Initialized?.Invoke(data); registry.Register(this);
        roleAbility = EnemyRoleAbility.Configure(this,data,registry,roleAbility);
        if(!data.HasConfiguredBaseDamage) Debug.LogWarning(data.DisplayName+": endpoint damage is not configured (design says 'tower health'); Base arrival retires safely without damage or a kill reward.",this);
    }
    public void ReceiveDamage(float amount) => ReceiveDamage(amount,false);
    public void ReceiveDamage(float amount, bool bypassShield) {
        if (IsAlive && amount > 0 && !float.IsNaN(amount) && !float.IsInfinity(amount))
            health.ReceiveDamage(bypassShield ? amount : registry.ResolveDamage(this,amount));
    }
    protected virtual void OnCombatDeath() { }
    public EnemyStatusEffects StatusEffects {
        get { var effects = GetComponent<EnemyStatusEffects>(); return effects != null ? effects : gameObject.AddComponent<EnemyStatusEffects>(); }
    }
    protected virtual void Die() {
        if (!active) return;
        IsDead = true;
        OnCombatDeath();
        RetireCombat();
        ApplyDeadTransparency();
        Died?.Invoke(this);
    }
    private void ReachBase() {
        if (active) {
            Retire();
            if (Definition != null && Definition.HasConfiguredBaseDamage && baseTarget != null)
                baseTarget.ReceiveDamage(baseDamage);
            Destroy(gameObject);
        } else if (IsDead) {
            if (movement != null) movement.Stop();
            Destroy(gameObject);
        }
    }
    private void OnMoved() { if (active) registry.NotifyMoved(this); }
    private void RetireCombat() {
        active = false;
        if (roleAbility != null) roleAbility.Shutdown();
        var effects = GetComponent<EnemyStatusEffects>();
        if (effects != null) effects.Clear();
        if (movement != null) {
            movement.Moved -= OnMoved;
            movement.Modifiers.Clear();
        }
        if (registry != null) registry.Unregister(this);
    }
    private void Retire() {
        RetireCombat();
        if (movement != null) movement.Stop();
    }
    private void ApplyDeadTransparency() {
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>(true)) {
            var color = sprite.color;
            color.a = DeadSpriteAlpha;
            sprite.color = color;
        }
    }
    protected virtual void OnDisable() {
        if (active) Retire();
        else if (movement != null) movement.Stop();
    }
    protected virtual void OnDestroy() {
        if (health != null) health.Depleted -= Die;
        if (movement != null) movement.ReachedEnd -= ReachBase;
        if (registry != null) registry.Unregister(this);
    }
}
}
