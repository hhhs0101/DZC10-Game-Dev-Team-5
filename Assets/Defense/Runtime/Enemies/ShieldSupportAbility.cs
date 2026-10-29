using UnityEngine;
namespace Defense {
public sealed class ShieldSupportAbility : SupportEnemyAbility {
    // Compatibility property for existing inspector/test consumers; no finite pool remains.
    public float RemainingShield => IsOperational ? Data.ShieldValue : 0;
    protected override void OnActivated() => ShieldEnemies();
    public void ShieldEnemies() {
        if (IsOperational) Registry.RegisterShield(this);
    }
    protected override void OnDeactivated() { if(Registry != null) Registry.UnregisterShield(this); }
    public float AbsorbDamage(Enemy target, float pending) {
        bool protectedTarget = IsOperational && target != null && target.IsAlive &&
            PlanarSpace.SqrDistance(Owner.transform.position,target.transform.position,Registry.Plane) <= Radius*Radius;
        return protectedTarget ? pending * (1 - Mathf.Clamp01(Data.ShieldValue)) : pending;
    }
}
}
