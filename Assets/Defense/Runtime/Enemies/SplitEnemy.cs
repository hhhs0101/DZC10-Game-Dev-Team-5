using System.Collections.Generic;
namespace Defense {
public sealed class SplitEnemy : Enemy {
    private bool splitCommitted;
    private void OnEnable() => splitCommitted = false;
    public List<Enemy> Split() {
        var children = new List<Enemy>();
        if(splitCommitted || !CanSplit || CurrentHealth > 0 || Spawner == null) return children;
        splitCommitted = true;
        // Spawn before unregistering the parent, so Victory cannot race the split.
        for(int i=0;i<Definition.SplitCount;i++) {
            var child = Spawner.SpawnChild(Definition,this,false);
            if(child != null) children.Add(child);
        }
        return children;
    }
    protected override void OnCombatDeath() => Split();
}
}
