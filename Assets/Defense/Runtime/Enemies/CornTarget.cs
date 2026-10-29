using UnityEngine;
namespace Defense {
// Plain section state: no Enemy inheritance, GameObject, movement, or reward lifecycle.
public sealed class CornTarget {
    private readonly ClusterEnemy owner;
    private bool active = true;
    public int Index { get; }
    public float CurrentHealth { get; private set; }
    public float MaximumHealth { get; }
    public bool IsAlive => active && owner != null && owner.IsAlive && CurrentHealth > 0;
    public TargetStatusEffects StatusEffects { get; }

    internal CornTarget(ClusterEnemy body, int index, float maximumHealth) {
        owner = body; Index = index;
        CurrentHealth = MaximumHealth = maximumHealth;
        StatusEffects = new TargetStatusEffects(body, () => IsAlive, amount => ReceiveDamage(amount));
    }

    public void ReceiveDamage(float amount, bool bypassShield = false) {
        if (!IsAlive || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        float resolved = owner.ResolveSectionDamage(amount, bypassShield);
        CurrentHealth = Mathf.Max(0, CurrentHealth - resolved);
        if (CurrentHealth <= 0) Retire();
        owner.SectionHealthChanged(this);
    }

    internal void Retire() {
        active = false;
        StatusEffects.Clear();
    }
}
}
