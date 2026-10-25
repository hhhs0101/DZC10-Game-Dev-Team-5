using UnityEngine;
namespace Defense {
public sealed class SupportTower : Tower {
    protected override bool IsSupportTower => true;
    protected override void PerformAttack(Enemy target, float damage) { }
    internal static void Advance(Tower tower, float deltaTime, ref float cleanseClock) {
        var definition = tower.Definition;
        var registry = tower.CombatRegistry;
        if(definition == null || !tower.isActiveAndEnabled || tower.TowerType != TowerType.Mayonnaise || tower.AttackDisabled || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        var flow = registry.GetComponent<GameFlow>(); if(flow != null && !flow.IsPlaying) return;
        cleanseClock += deltaTime;
        if(cleanseClock < definition.CleanseInterval) return;
        cleanseClock %= definition.CleanseInterval;
        tower.NotifySupportAttack();
        var choices = new System.Collections.Generic.List<Tower>();
        foreach(var slot in Object.FindObjectsByType<TowerPlacementSlot>()) {
            var candidate = slot.Occupant;
            if(candidate != null && candidate != tower && candidate.isActiveAndEnabled && candidate.AttackDisabled && candidate.gameObject.scene == tower.gameObject.scene &&
                PlanarSpace.SqrDistance(tower.transform.position,candidate.transform.position,registry.Plane) <= tower.EffectiveAttackRange*tower.EffectiveAttackRange) choices.Add(candidate);
        }
        if(choices.Count > 0) choices[Random.Range(0,choices.Count)].AttackStatus.Clear();
    }
}
}
