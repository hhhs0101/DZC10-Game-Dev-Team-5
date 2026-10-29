namespace Defense {
public sealed class BasicTower : AttackerTower {
    protected override void OnInitialized() {
        base.OnInitialized();
        SetDamageAction(DealDamage);
    }
    private void DealDamage(Enemy target, float damage) => target.ReceiveDamage(damage);
}
}
