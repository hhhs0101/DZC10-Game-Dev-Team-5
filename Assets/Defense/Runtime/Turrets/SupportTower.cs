using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class SupportTower : Tower {
    private float cleanseClock;
    public float DamageBoost => Definition != null ? Definition.DamageBoost : 1;
    public float CleanseInterval => Definition != null ? Definition.CleanseInterval : 0;
    protected override void OnInitialized() => cleanseClock = 0;
    protected override void AdvanceBehavior(float deltaTime) => AdvanceSupport(deltaTime);

    private bool Supports(Tower target) => Definition != null && isActiveAndEnabled && CurrentHealth > 0 && !AttackDisabled &&
        target != null && target != this && target.isActiveAndEnabled && target.CurrentHealth > 0 && target.gameObject.scene == gameObject.scene &&
        PlanarSpace.SqrDistance(transform.position,target.transform.position,CombatRegistry.Plane) <= AttackRange*AttackRange;

    // Compute current coverage on demand so moving, disabling or removing a source cannot leave stale boosts.
    internal static float GetDamageMultiplier(AttackerTower target) {
        float multiplier = 1;
        foreach (var slot in Object.FindObjectsByType<TowerPlacementSlot>()) {
            var support = slot.Occupant as SupportTower;
            if (support != null && support.Supports(target)) multiplier = Mathf.Max(multiplier,support.DamageBoost);
        }
        return multiplier;
    }
    public void AdvanceSupport(float deltaTime) {
        if (!CanAct || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        cleanseClock += deltaTime;
        if (cleanseClock < CleanseInterval) return;
        cleanseClock %= CleanseInterval;
        NotifyAttack(null);
        CleanseNearbyTower();
    }
    private void CleanseNearbyTower() {
        var choices = new List<Tower>();
        foreach (var slot in Object.FindObjectsByType<TowerPlacementSlot>()) {
            var candidate = slot.Occupant;
            if (Supports(candidate) && candidate.AttackDisabled) choices.Add(candidate);
        }
        if (choices.Count > 0) choices[Random.Range(0,choices.Count)].AttackStatus.Clear();
    }
}
}
