namespace Defense {
public abstract class StatusEffectTower : AttackTower {
    private readonly object statusToken = new object();
    protected object StatusToken => statusToken;
    protected override void OnInitialized() {
        base.OnInitialized();
        SetDamageAction(DealDamage);
    }
    private void DealDamage(Enemy target, float damage) {
        // ClusterEnemy keeps the hit and effect on the same section, even on a lethal hit.
        target.ReceiveStatusDamage(damage, effects => ApplyStatusEffect(effects,damage));
    }
    protected abstract void ApplyStatusEffect(TargetStatusEffects target, float damage);
}
}
