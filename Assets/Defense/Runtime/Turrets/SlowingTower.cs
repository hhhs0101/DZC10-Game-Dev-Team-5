using UnityEngine;
namespace Defense {
public sealed class SlowingTower : StatusEffectTower {
    protected override void ApplyStatusEffect(Enemy target, float damage) => Slow(this,target);
    internal static void Slow(Tower tower, Enemy target) {
        var definition = tower.Definition;
        target.StatusEffects.ApplySlow(tower.StatusToken,definition.SlowMultiplier,definition.SlowDuration);
    }
}
}
