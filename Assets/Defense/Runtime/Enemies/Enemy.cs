using System;
using UnityEngine;
namespace Defense {
[RequireComponent(typeof(EnemyHealth), typeof(EnemyPathFollower))]
public abstract class Enemy : MonoBehaviour, IDamageable {
    public const float DeadSpriteAlpha = 0.4f;
    private static readonly Vector3 TunnelHiddenPosition = new Vector3(0f, -1000f, 0f);
    private EnemyHealth health;
    private EnemyPathFollower movement;
    private EnemyRegistry registry;
    private IDamageable baseTarget;
    private int baseDamage;
    private bool active;
    private EnemyRoleAbility roleAbility;
    private Action<float> damageHandler, piercingHandler, splashHandler;
    private Action<float,Action<TargetStatusEffects>> statusHandler;
    protected virtual void OnCombatInitialized(EnemyHealth health) { }
    protected virtual void OnCombatRetired() { }
    public bool IsAlive => active && isActiveAndEnabled && health.Current > 0;
    public bool IsDead { get; private set; }
    public float CurrentHealth => health != null ? health.Current : 0;
    public float MaximumHealth => health != null ? health.Maximum : 0;
    public EnemySpawner Spawner { get; internal set; }
    public bool CanSplit { get; internal set; } = true;
    public EnemyRegistry Registry => registry;
    public Vector2 GetPosition() => transform.position;
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
        if(movement != null) {
            movement.ReachedEnd -= ReachBase;
            movement.Moved -= OnMoved;
            movement.EnteredTunnel -= OnEnteredTunnel;
            movement.ExitedTunnel -= OnExitedTunnel;
        }
        health = GetComponent<EnemyHealth>(); movement = GetComponent<EnemyPathFollower>();
        registry = enemies; baseTarget = target; baseDamage = data.BaseDamage;
        Definition = data; ResourceReward = data.ResourceReward;
        health.Initialize(data.Health);
        health.Depleted += Die;
        movement.ReachedEnd += ReachBase;
        movement.EnteredTunnel += OnEnteredTunnel;
        movement.ExitedTunnel += OnExitedTunnel;
        movement.Initialize(path, data.MovementSpeed);
        if(continuation != null) movement.ContinueFrom(continuation);
        movement.Moved += OnMoved;
        IsDead = false;
        SetVisualVisibility(true);
        active = true;
        SetDamageHandlers(HandleDamage,HandlePiercingDamage,HandleDamage,HandleStatusDamage);
        OnCombatInitialized(health);
        Initialized?.Invoke(data); registry.Register(this);
        roleAbility = EnemyRoleAbility.Configure(this,data,registry,roleAbility);
        if(!data.HasConfiguredBaseDamage) Debug.LogWarning(data.DisplayName+": endpoint damage is not configured (design says 'tower health'); Base arrival retires safely without damage or a kill reward.",this);
    }
    protected void SetDamageHandlers(Action<float> direct, Action<float> piercing,
        Action<float> splash, Action<float,Action<TargetStatusEffects>> status) {
        damageHandler = direct ?? throw new ArgumentNullException(nameof(direct));
        piercingHandler = piercing ?? throw new ArgumentNullException(nameof(piercing));
        splashHandler = splash ?? throw new ArgumentNullException(nameof(splash));
        statusHandler = status ?? throw new ArgumentNullException(nameof(status));
    }
    private bool CanReceive(float amount) => IsAlive && amount > 0 && !float.IsNaN(amount) && !float.IsInfinity(amount);
    public void ReceiveDamage(float amount) { if (CanReceive(amount)) damageHandler(amount); }
    public void ReceivePiercingDamage(float amount) { if (CanReceive(amount)) piercingHandler(amount); }
    public void ReceiveSplashDamage(float amount) { if (CanReceive(amount)) splashHandler(amount); }
    public void ReceiveStatusDamage(float amount, Action<TargetStatusEffects> applyEffect) {
        if (IsAlive && amount >= 0 && !float.IsNaN(amount) && !float.IsInfinity(amount))
            statusHandler(amount,applyEffect);
    }
    private void HandleDamage(float amount) => health.ReceiveDamage(registry.ResolveDamage(this,amount));
    private void HandlePiercingDamage(float amount) => health.ReceiveDamage(amount);
    private void HandleStatusDamage(float amount, Action<TargetStatusEffects> applyEffect) {
        if (amount > 0) HandleDamage(amount);
        if (IsAlive) applyEffect?.Invoke(StatusEffects.State);
    }
    protected virtual void OnCombatDeath() { }
    public EnemyStatusEffects StatusEffects {
        get { var effects = GetComponent<EnemyStatusEffects>(); return effects != null ? effects : gameObject.AddComponent<EnemyStatusEffects>(); }
    }
    protected virtual void Die() {
        if (!active) return;
        IsDead = true;
        GameAudio.PlayEnemyNoHp();
        OnCombatDeath();
        RetireCombat();
        ApplyDeadTransparency();
        Died?.Invoke(this);
    }
    private void ReachBase() {
        if (active) {
            GameAudio.PlayEnemyAliveReaching();
            Retire();
            if (Definition != null && Definition.HasConfiguredBaseDamage && baseTarget != null)
                baseTarget.ReceiveDamage(baseDamage);
            Destroy(gameObject);
        } else if (IsDead) {
            GameAudio.PlayEnemyDeadReaching();
            if (movement != null) movement.Stop();
            Destroy(gameObject);
        }
    }
    private void OnMoved() { if (active) registry.NotifyMoved(this); }
    private void OnEnteredTunnel() {
        transform.position = TunnelHiddenPosition;
        SetVisualVisibility(false);
        if (roleAbility != null) roleAbility.enabled = false;
        if (active && registry != null) registry.NotifyMoved(this);
    }
    private void OnExitedTunnel() {
        SetVisualVisibility(true);
        if (active && roleAbility != null) roleAbility.enabled = true;
        if (active && registry != null) registry.NotifyMoved(this);
    }
    private void SetVisualVisibility(bool visible) {
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>(true))
            sprite.enabled = visible;
        var healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar != null) healthBar.enabled = visible && !IsDead;
    }
    private void RetireCombat() {
        active = false;
        OnCombatRetired();
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
        if (movement != null) {
            movement.ReachedEnd -= ReachBase;
            movement.EnteredTunnel -= OnEnteredTunnel;
            movement.ExitedTunnel -= OnExitedTunnel;
        }
        if (registry != null) registry.Unregister(this);
    }
}
}
