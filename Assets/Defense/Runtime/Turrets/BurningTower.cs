using UnityEngine;
namespace Defense {
public sealed class BurningTower : StatusEffectTower {
    protected override void ApplyStatusEffect(Enemy target, float damage) => Burn(this,target,damage);
    internal static void Burn(Tower tower, Enemy target, float damage) {
        var definition = tower.Definition;
        target.StatusEffects.ApplyBurn(tower.StatusToken,definition.BurnDamagePerSecond * (definition.Damage > 0 ? damage / definition.Damage : 1),definition.BurnDuration);
    }
}
}
