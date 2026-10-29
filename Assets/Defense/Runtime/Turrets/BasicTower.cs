namespace Defense {
public sealed class BasicTower : AttackTower {
    protected override void OnInitialized() {
        base.OnInitialized();
        SetDamageAction(DealDamage);
    }
    private void DealDamage(Enemy target, float damage) => target.ReceiveDamage(damage);
}
}
