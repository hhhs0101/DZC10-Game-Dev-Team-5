using UnityEngine;
namespace Defense {
public sealed class ShieldPiercingTower : Tower {
    protected override void PerformAttack(Enemy target, float damage) => DealPiercingDamage(target,damage);
    internal static void DealPiercingDamage(Enemy target, float damage) => target.ReceiveDamage(damage,true);
}
}
