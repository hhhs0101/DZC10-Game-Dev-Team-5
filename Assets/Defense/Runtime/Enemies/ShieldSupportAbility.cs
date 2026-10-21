using UnityEngine;
namespace Defense {
public sealed class ShieldSupportAbility : SupportEnemyAbility {
    [SerializeField, Tooltip("Runtime debug state: one finite shared pool, not one shield per ally.")]
    private float remainingShield;
    public float RemainingShield => remainingShield;
    protected override void ResetState() => remainingShield = Mathf.Max(0,Data.ShieldValue);
    protected override void OnActivated() => Registry.RegisterShield(this);
    protected override void OnDeactivated() { if(Registry != null) Registry.UnregisterShield(this); }
    // No recursive damage calls or death events: only the original target's remainder reaches health.
    public float AbsorbDamage(Enemy target, float pending) {
        if(pending <= 0 || remainingShield <= 0 || !Protects(target)) return pending;
        float absorbed = Mathf.Min(remainingShield,pending);
        remainingShield -= absorbed; return pending-absorbed;
    }
}
}
