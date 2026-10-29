namespace Defense {
// A child enemy with one health pool. All four damage methods are inherited from Enemy.
public sealed class CornTarget : Enemy {
    private ClusterEnemy owner;
    private EnemyHealth sectionHealth;
    public int index { get; private set; }

    internal void InitializeSection(ClusterEnemy body, int index, float maximumHealth) {
        owner = body; this.index = index;
        InitializeOwnedTarget(body,maximumHealth);
        sectionHealth = GetComponent<EnemyHealth>();
        sectionHealth.Changed += OnHealthChanged;
    }
    private void OnHealthChanged(float current, float maximum) => owner.SectionHealthChanged(this);
    internal void Retire() {
        if (sectionHealth != null) sectionHealth.Changed -= OnHealthChanged;
        RetireOwnedTarget();
    }
    protected override void OnDestroy() {
        if (sectionHealth != null) sectionHealth.Changed -= OnHealthChanged;
        base.OnDestroy();
    }
}
}
