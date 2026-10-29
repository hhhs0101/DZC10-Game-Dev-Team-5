using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// One moving enemy with independently damageable sections.
public sealed class ClusterEnemy : Enemy {
    private readonly List<CornTarget> targets = new List<CornTarget>();
    private bool targetsActive;
    public IReadOnlyList<CornTarget> Targets => targets;
    public override bool IsCombatTarget => false;
    protected override int HealthLayerCount => Definition.SplitCount;

    public override Enemy SelectCombatTarget() {
        if (!targetsActive || !IsAlive) return null;
        foreach (var target in targets)
            if (target != null && target.IsAlive) return target;
        return null;
    }

    protected override void OnCombatInitialized(EnemyHealth health) {
        OnCombatRetired();
        foreach (var target in targets) if (target != null) {
            target.gameObject.SetActive(false);
            Destroy(target.gameObject);
        }
        targets.Clear();
        targetsActive = true;
        for (int i = 0; i < health.LayerCount; i++) {
            var child = new GameObject("Combat Target " + (i + 1), typeof(EnemyHealth), typeof(EnemyPathFollower));
            child.transform.SetParent(transform, false);
            var target = child.AddComponent<CornTarget>();
            target.InitializeCombatTarget(this, health.LayerMaximum);
            targets.Add(target);
            int index = i;
            target.GetComponent<EnemyHealth>().Changed += (current, maximum) => {
                if (targetsActive) health.SetLayerHealth(index, current);
            };
        }
    }

    public override void ReceiveDamage(float amount, bool bypassShield) {
        var target = SelectCombatTarget();
        if (target != null) target.ReceiveDamage(amount, bypassShield);
    }

    public override void ReceiveSplashDamage(float amount) {
        if (!targetsActive || !IsAlive) return;
        foreach (var target in targets)
            if (target != null && target.IsAlive) target.ReceiveSplashDamage(amount);
    }

    protected override void OnCombatRetired() {
        targetsActive = false;
        foreach (var target in targets)
            if (target != null) target.RetireCombatTarget();
    }

    protected override void OnDestroy() {
        OnCombatRetired();
        base.OnDestroy();
    }
}
}
