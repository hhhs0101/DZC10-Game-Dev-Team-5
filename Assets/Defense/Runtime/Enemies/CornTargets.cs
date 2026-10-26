using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class CornTargets : MonoBehaviour {
    private readonly List<CornTarget> targets = new List<CornTarget>();
    private Enemy body;
    private EnemyHealth bodyHealth;
    private bool running;
    public IReadOnlyList<CornTarget> Targets => targets;
    public Enemy FirstLivingTarget {
        get {
            if(!running) return null;
            foreach(var target in targets) if(target != null && target.IsAlive) return target;
            return null;
        }
    }
    internal void Initialize(Enemy owner, EnemyHealth health) {
        Retire();
        foreach(var target in targets) if(target != null) {
            target.gameObject.SetActive(false);
            Destroy(target.gameObject);
        }
        targets.Clear(); body = owner; bodyHealth = health; running = true;
        for(int i = 0; i < health.LayerCount; i++) {
            var child = new GameObject("Corn Target " + (i + 1),typeof(EnemyHealth),typeof(EnemyPathFollower));
            child.transform.SetParent(transform,false);
            var target = child.AddComponent<CornTarget>();
            target.InitializeCombatTarget(body,health.LayerMaximum);
            targets.Add(target);
            int index = i;
            target.GetComponent<EnemyHealth>().Changed += (current, maximum) => {
                if(running) bodyHealth.SetLayerHealth(index,current);
            };
        }
    }
    public void ReceiveDamage(float amount, bool bypassShield = false) {
        if(!running || !body.IsAlive) return;
        foreach(var target in targets) if(target != null && target.IsAlive) {
            target.ReceiveDamage(amount,bypassShield);
            return;
        }
    }
    public void ReceiveSplashDamage(float amount) {
        if(!running || !body.IsAlive) return;
        foreach(var target in targets)
            if(target != null && target.IsAlive) target.ReceiveSplashDamage(amount);
    }
    internal void Retire() {
        running = false;
        foreach(var target in targets) if(target != null) target.RetireCombatTarget();
    }
    private void OnDisable() => Retire();
    private void OnDestroy() => Retire();
}
}
