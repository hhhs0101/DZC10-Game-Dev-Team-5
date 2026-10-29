namespace Defense {
public sealed class ShieldPiercingTower : AttackerTower {
    protected override void OnInitialized() {
        base.OnInitialized();
        SetDamageAction(DealPiercingDamage);
    }
    private void DealPiercingDamage(Enemy target, float damage) => target.ReceiveDamage(damage,true);
}
}
