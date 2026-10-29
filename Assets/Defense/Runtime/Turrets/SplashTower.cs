using System.Collections.Generic;
namespace Defense {
public sealed class SplashTower : AttackTower {
    protected override void OnInitialized() {
        base.OnInitialized();
        SetDamageAction(DealSplashDamage);
    }
    private void DealSplashDamage(Enemy target, float damage) {
        var center = target.transform.position;
        // Death unregisters synchronously, so iterate a snapshot of bodies.
        var victims = new List<Enemy>(CombatRegistry.Enemies);
        foreach (var enemy in victims)
            if (enemy != null && enemy.IsAlive && PlanarSpace.SqrDistance(center,enemy.transform.position,CombatRegistry.Plane) <= Definition.SplashRadius*Definition.SplashRadius)
                enemy.ReceiveSplashDamage(damage);
    }
}
}
