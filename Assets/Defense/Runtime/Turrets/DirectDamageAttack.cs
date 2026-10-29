namespace Defense {
public sealed class DirectDamageAttack : AttackBehaviour {
    public override void Attack(CombatTarget target, float damage) {
        if (target != null && target.IsAlive) target.ReceiveDamage(damage);
    }
}
}
