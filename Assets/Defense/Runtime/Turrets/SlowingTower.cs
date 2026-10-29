namespace Defense {
public sealed class SlowingTower : StatusEffectTower {
    protected override void ApplyEffect(TargetStatusEffects effects, float damage) =>
        effects.ApplySlow(StatusToken,Definition.SlowMultiplier,Definition.SlowDuration);
}
}
