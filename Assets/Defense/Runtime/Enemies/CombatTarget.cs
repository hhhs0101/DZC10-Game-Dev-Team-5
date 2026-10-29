using System;
using UnityEngine;
namespace Defense {
// A stable attack address. Consumers do not need to know how a body stores its sections.
public sealed class CombatTarget {
    private readonly Func<bool> alive;
    private readonly Action<float, bool> damage;
    private readonly Action<float> splash;
    private readonly Func<TargetStatusEffects> effects;
    public Enemy Body { get; }
    public int SectionIndex { get; }
    public Vector3 Position => Body != null ? Body.transform.position : Vector3.zero;
    public bool IsAlive => Body != null && alive();
    public TargetStatusEffects StatusEffects => effects();
    internal CombatTarget(Enemy body, int sectionIndex, Func<bool> isAlive,
        Action<float, bool> receiveDamage, Action<float> receiveSplashDamage,
        Func<TargetStatusEffects> statusEffects) {
        Body = body; SectionIndex = sectionIndex; alive = isAlive;
        damage = receiveDamage; splash = receiveSplashDamage; effects = statusEffects;
    }
    public void ReceiveDamage(float amount, bool bypassShield = false) {
        if (IsAlive) damage(amount, bypassShield);
    }
    public void ReceiveSplashDamage(float amount) {
        if (IsAlive) splash(amount);
    }
}
}
