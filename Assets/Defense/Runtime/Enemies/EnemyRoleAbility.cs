using System;
using UnityEngine;
namespace Defense {
// Composition keeps the shared BasicEnemy actor/prefab and its existing lifecycle intact.
public abstract class EnemyRoleAbility : MonoBehaviour {
    protected Enemy Owner { get; private set; }
    protected EnemyRegistry Registry { get; private set; }
    protected EnemyDefinition Data { get; private set; }
    protected GameFlow Flow { get; private set; }
    private bool initialized, running;
    public bool IsOperational => running && Owner != null && Owner.IsAlive && Data.AbilityConfigured;
    public static EnemyRoleAbility Configure(Enemy owner, EnemyDefinition data, EnemyRegistry registry, EnemyRoleAbility current) {
        Type type = null;
        switch(data.Role) {
            case EnemyRole.SpeedSupport: type = typeof(SpeedSupportAbility); break;
            case EnemyRole.ShieldSupport: type = typeof(ShieldSupportAbility); break;
            case EnemyRole.Disruptor: type = typeof(TowerDisruptionAbility); break;
        }
        if(current != null) {
            current.Shutdown();
            if(current.GetType() != type) { Destroy(current); current = null; }
        }
        if(type == null) return null;
        if(current == null) current = (EnemyRoleAbility)owner.gameObject.AddComponent(type);
        current.Owner = owner; current.Data = data; current.Registry = registry;
        current.Flow = registry.GetComponent<GameFlow>(); current.ResetState(); current.initialized = true;
        if(!data.AbilityConfigured) Debug.LogWarning(data.DisplayName+": "+data.AbilityConfigurationIssue,owner);
        if(!current.enabled) current.enabled = true; else current.Activate();
        return current;
    }
    private void Activate() {
        if(running || !initialized || Owner == null || !Owner.IsAlive || !Data.AbilityConfigured) return;
        running = true; OnActivated();
    }
    private void Deactivate() { if(!running) return; running = false; OnDeactivated(); }
    public void Shutdown() { Deactivate(); initialized = false; }
    protected virtual void ResetState() { }
    protected virtual void OnActivated() { }
    protected virtual void OnDeactivated() { }
    private void OnEnable() => Activate();
    private void OnDisable() => Deactivate();
    private void OnDestroy() => Shutdown();
}
}
