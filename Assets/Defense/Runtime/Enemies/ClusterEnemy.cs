using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// The registered moving body owns five child enemies with independent health.
public sealed class ClusterEnemy : Enemy {
    private readonly List<CornTarget> targets = new List<CornTarget>();
    private EnemyHealth bodyHealth;
    private bool targetsActive;
    public IReadOnlyList<CornTarget> Targets => targets;

    private CornTarget FirstLivingTarget() {
        if (!targetsActive || !IsAlive) return null;
        foreach (var target in targets)
            if (target.IsAlive) return target;
        return null;
    }

    protected override void OnCombatInitialized(EnemyHealth health) {
        OnCombatRetired();
        foreach (var oldTarget in targets) if (oldTarget != null) {
            oldTarget.gameObject.SetActive(false);
            Destroy(oldTarget.gameObject);
        }
        targets.Clear();
        bodyHealth = health;
        health.Initialize(Definition.Health,Definition.SplitCount);
        targetsActive = true;
        for (int i = 0; i < health.LayerCount; i++) {
            var child = new GameObject("Corn Target " + (i+1));
            child.transform.SetParent(transform,false);
            var target = child.AddComponent<CornTarget>();
            target.InitializeSection(this,i,health.LayerMaximum);
            targets.Add(target);
        }
        SetDamageHandlers(DamageFirstTarget,PierceFirstTarget,DamageAllTargets,DamageAndApplyEffect);
        SetBaseDamageHandler(CalculateBaseDamage);
    }

    private int CalculateBaseDamage() {
        int survivors = 0;
        foreach (var target in targets)
            if (target.IsAlive) survivors++;
        return Definition.BaseDamage * survivors;
    }

    private void DamageFirstTarget(float amount) {
        FirstLivingTarget()?.ReceiveDamage(amount);
    }

    private void PierceFirstTarget(float amount) => FirstLivingTarget()?.ReceivePiercingDamage(amount);

    private void DamageAndApplyEffect(float amount, Action<TargetStatusEffects> applyEffect) {
        var section = FirstLivingTarget();
        if (section == null) return;
        section.ReceiveStatusDamage(amount,applyEffect);
    }

    private void DamageAllTargets(float amount) {
        if (!targetsActive || !IsAlive) return;
        foreach (var target in targets)
            if (target.IsAlive) target.ReceiveSplashDamage(amount);
    }

    internal void SectionHealthChanged(CornTarget target) {
        if (targetsActive && target.index < targets.Count && ReferenceEquals(targets[target.index], target))
            bodyHealth.SetLayerHealth(target.index, target.CurrentHealth);
    }

    public void AdvanceSections(float deltaTime) {
        if (!targetsActive || !IsAlive) return;
        foreach (var target in targets) target.StatusEffects.Advance(deltaTime);
    }
    private void Update() => AdvanceSections(Time.deltaTime);

    protected override void OnCombatRetired() {
        targetsActive = false;
        foreach (var target in targets) if (target != null) target.Retire();
    }

    protected override void OnDestroy() {
        OnCombatRetired();
        base.OnDestroy();
    }
}
}
