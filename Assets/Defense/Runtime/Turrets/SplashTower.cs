using UnityEngine;
namespace Defense {
public sealed class SplashTower : Tower {
    protected override void PerformAttack(Enemy target, float damage) => DealSplashDamage(this,target,damage);
    internal static void DealSplashDamage(Tower tower, Enemy target, float damage) {
        var center = target.transform.position;
        var registry = tower.CombatRegistry;
        var definition = tower.Definition;
        // Snapshot membership before any damage can mutate the registry.
        var victims = new System.Collections.Generic.List<Enemy>(registry.CombatTargets);
        foreach(var enemy in victims) if(enemy != null && enemy.IsAlive && PlanarSpace.SqrDistance(center,enemy.transform.position,registry.Plane) <= definition.SplashRadius*definition.SplashRadius) enemy.ReceiveSplashDamage(damage);
    }
}
}
