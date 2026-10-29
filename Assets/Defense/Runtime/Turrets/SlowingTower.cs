namespace Defense {
public sealed class SlowingTower : StatusEffectTower {
    protected override void ApplyStatusEffect(TargetStatusEffects target, float damage) =>
        target.ApplySlow(StatusToken,Definition.SlowMultiplier,Definition.SlowDuration);
}
}
