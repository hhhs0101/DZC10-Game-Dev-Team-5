using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class BossEnemy : BasicEnemy {
    private float summonClock, attackClock;
    private void OnEnable() { summonClock = attackClock = 0; }
    private void Update() => AdvanceAbilities(Time.deltaTime);
    public void AdvanceAbilities(float deltaTime) {
        if(!IsAlive || Spawner == null || !Spawner.CanSpawn || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        summonClock += deltaTime; attackClock += deltaTime;
        while(summonClock >= Definition.SummonInterval) { summonClock -= Definition.SummonInterval; SummonEnemies(); }
        if(attackClock < Definition.TowerAttackInterval) return;
        attackClock %= Definition.TowerAttackInterval;
        Turret nearest = null; float distance = Definition.TowerAttackRange * Definition.TowerAttackRange;
        foreach(var slot in Object.FindObjectsByType<TowerPlacementSlot>()) {
            var tower = slot.Occupant;
            if(tower == null || !tower.isActiveAndEnabled || tower.gameObject.scene != gameObject.scene) continue;
            float d = PlanarSpace.SqrDistance(transform.position,tower.transform.position,Registry.Plane);
            if(d <= distance) { nearest = tower; distance = d; }
        }
        if(nearest != null) nearest.ReceiveDamage(nearest.MaximumHealth * Definition.TowerDamageFraction);
    }
    public void SummonEnemies() {
        if(!IsAlive || Spawner == null || !Spawner.CanSpawn) return;
        var choices = new List<EnemyDefinition>();
        foreach(var data in Definition.SummonTypes) if(data != null && data.Role != EnemyRole.Boss && data.Prefab != null && !choices.Contains(data)) choices.Add(data);
        if(choices.Count == 0) return;
        // Choose distinct types per wave when enough definitions are available.
        var pool = new List<EnemyDefinition>(choices);
        for(int i=0;i<Definition.SummonCount;i++) {
            if(pool.Count == 0) pool.AddRange(choices);
            int index = Random.Range(0,pool.Count);
            Spawner.SpawnChild(pool[index],this,true); pool.RemoveAt(index);
        }
    }
}
}
