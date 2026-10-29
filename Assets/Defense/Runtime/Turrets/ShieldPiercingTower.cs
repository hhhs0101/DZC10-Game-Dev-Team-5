namespace Defense {
public sealed class ShieldPiercingTower : AttackTower {
    protected override void OnInitialized() {
        base.OnInitialized();
        SetDamageAction(DealPiercingDamage);
    }
    private void DealPiercingDamage(Enemy target, float damage) => target.ReceivePiercingDamage(damage);
}
}
