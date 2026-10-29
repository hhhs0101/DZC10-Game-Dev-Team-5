using System;
using UnityEngine;
namespace Defense {
public class AttackerTower : Tower {
    private ITargetingStrategy targeting;
    private float cooldown;
    private Action<Enemy,float> damageAction;
    public float Damage => Definition != null ? Definition.Damage : 0;
    public float FinalDamage => Damage * SupportTower.GetDamageMultiplier(this);
    protected override void OnInitialized() {
        targeting = Definition.Targeting;
        cooldown = 0;
        // A plain AttackerTower is usable; specialized subclasses replace this callback.
        SetDamageAction((target, damage) => target.ReceiveDamage(damage));
    }
    protected override void AdvanceBehavior(float deltaTime) {
        cooldown = Mathf.Max(0,cooldown-deltaTime);
        if (!CanAct || cooldown > 0) return;
        var target = FindTarget();
        if (target == null) return;
        Attack(target);
        cooldown = Definition.AttackCooldown;
    }
    public Enemy FindTarget() => targeting != null && CombatRegistry != null
        ? targeting.Select(transform.position,AttackRange,CombatRegistry.Enemies,CombatRegistry.Plane) : null;
    public void Attack(Enemy target) {
        if (!CanAct || target == null || !target.IsAlive) return;
        damageAction(target,FinalDamage);
        NotifyAttack(target);
    }
    protected void SetDamageAction(Action<Enemy,float> action) {
        damageAction = action ?? throw new ArgumentNullException(nameof(action));
    }
}
}
