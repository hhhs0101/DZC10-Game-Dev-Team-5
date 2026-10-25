namespace Defense {
// Shared/basic prefab support for existing authoring callers.
// Specialized definitions use the dedicated subclasses.
public sealed class BasicTower : Tower {
    protected override bool IsSupportTower => TowerType == TowerType.Mayonnaise;
    protected override void PerformAttack(Enemy target, float damage) {
        switch(TowerType) {
            case TowerType.Ketchup: SplashTower.DealSplashDamage(this,target,damage); break;
            case TowerType.Mustard: ShieldPiercingTower.DealPiercingDamage(target,damage); break;
            case TowerType.HotSauce:
                target.ReceiveDamage(damage);
                if(target.IsAlive) BurningTower.Burn(this,target,damage);
                break;
            case TowerType.Oil:
                target.ReceiveDamage(damage);
                if(target.IsAlive) SlowingTower.Slow(this,target);
                break;
            case TowerType.Mayonnaise: break;
            default: DealDirectDamage(this,target,damage); break;
        }
    }
    internal static void DealDirectDamage(Tower tower, Enemy target, float damage) {
        if(tower.AttackComponent != null) tower.AttackComponent.Attack(target,damage);
        else target.ReceiveDamage(damage);
    }
}
}
