using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// The only enemy actor for the cluster. Sections are plain owned objects.
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
        targets.Clear();
        bodyHealth = health;
        health.Initialize(Definition.Health,Definition.SplitCount);
        targetsActive = true;
        for (int i = 0; i < health.LayerCount; i++) {
            var target = new CornTarget(this, i, health.LayerMaximum);
            targets.Add(target);
        }
        SetDamageHandlers(DamageFirstTarget,PierceFirstTarget,DamageAllTargets,DamageAndApplyEffect);
    }

    private void DamageFirstTarget(float amount) {
        FirstLivingTarget()?.ReceiveDamage(amount);
    }

    private void PierceFirstTarget(float amount) => FirstLivingTarget()?.ReceiveDamage(amount,true);

    private void DamageAndApplyEffect(float amount, Action<TargetStatusEffects> applyEffect) {
        var section = FirstLivingTarget();
        if (section == null) return;
        section.ReceiveDamage(amount);
        if (section.IsAlive) applyEffect?.Invoke(section.StatusEffects);
    }

    private void DamageAllTargets(float amount) {
        if (!targetsActive || !IsAlive) return;
        foreach (var target in targets)
            if (target.IsAlive) target.ReceiveDamage(amount);
    }

    internal float ResolveSectionDamage(float amount, bool bypassShield) =>
        bypassShield ? amount : Registry.ResolveDamage(this, amount);

    internal void SectionHealthChanged(CornTarget target) {
        if (targetsActive && target.Index < targets.Count && ReferenceEquals(targets[target.Index], target))
            bodyHealth.SetLayerHealth(target.Index, target.CurrentHealth);
    }

    public void AdvanceSections(float deltaTime) {
        if (!targetsActive || !IsAlive) return;
        foreach (var target in targets) target.StatusEffects.Advance(deltaTime);
    }
    private void Update() => AdvanceSections(Time.deltaTime);

    protected override void OnCombatRetired() {
        targetsActive = false;
        foreach (var target in targets) target.Retire();
    }

    protected override void OnDestroy() {
        OnCombatRetired();
        base.OnDestroy();
    }
}
}
