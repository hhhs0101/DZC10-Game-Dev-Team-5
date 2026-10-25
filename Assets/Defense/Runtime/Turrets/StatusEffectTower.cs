using UnityEngine;
namespace Defense {
public abstract class StatusEffectTower : Tower {
    protected override void PerformAttack(Enemy target, float damage) {
        target.ReceiveDamage(damage);
        if(target.IsAlive) ApplyStatusEffect(target,damage);
    }
    protected abstract void ApplyStatusEffect(Enemy target, float damage);
}
}
