namespace Defense {
// Shared lifecycle, movement and damage stay in Enemy. Each generation is defined by data.
public sealed class DeathSplitEnemy : BasicEnemy {
    protected override void OnCombatDeath() {
        base.OnCombatDeath();
        var next = Definition.DeathSplitChild;
        if (!CanSplit || next == null || Spawner == null || !Spawner.CanSpawn) return;
        // Spawn synchronously before the parent retires so Victory never sees an empty registry.
        for (int i=0;i<Definition.DeathSplitCount;i++) {
            var child = Spawner.SpawnChild(next,this,next.DeathSplitChild != null);
            // Follow the same route rather than offsetting enemies off the track.
            if (child != null && i>0 && child.Movement.EffectiveMovementSpeed>0)
                child.Movement.Advance(i*Definition.DeathSplitSpacing/child.Movement.EffectiveMovementSpeed);
        }
    }
}
}
