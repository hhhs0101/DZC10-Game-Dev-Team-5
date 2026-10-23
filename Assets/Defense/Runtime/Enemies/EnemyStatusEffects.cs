using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// Target-owned timers survive leaving tower range and the attacking tower's destruction.
public sealed class EnemyStatusEffects : MonoBehaviour {
    private sealed class Burn { public object Source; public float Dps, Remaining; }
    private readonly List<Burn> burns = new List<Burn>();
    private sealed class Slow { public object Source; public float Remaining; }
    private readonly List<Slow> slows = new List<Slow>();
    private Enemy owner;
    private void Awake() => owner = GetComponent<Enemy>();
    public void ApplyBurn(object source, float dps, float seconds) {
        if(source == null || dps <= 0 || seconds <= 0) return;
        var burn = burns.Find(b => ReferenceEquals(b.Source,source));
        if(burn == null) { burn = new Burn { Source = source }; burns.Add(burn); }
        burn.Dps = dps; burn.Remaining = seconds;
    }
    public void ApplySlow(object source, float multiplier, float seconds) {
        if(source == null || seconds <= 0) return;
        var slow = slows.Find(s => ReferenceEquals(s.Source,source));
        if(slow == null) { slow = new Slow { Source = source }; slows.Add(slow); }
        slow.Remaining = seconds;
        owner.Movement.Modifiers.SetSlow(source,multiplier);
    }
    private void Update() => Advance(Time.deltaTime);
    public void Advance(float deltaTime) {
        if(owner == null || !owner.IsAlive || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        var flow = owner.Registry.GetComponent<GameFlow>(); if(flow != null && !flow.IsPlaying) return;
        float damage = 0;
        for(int i=burns.Count-1;i>=0;i--) {
            var burn = burns[i]; damage += burn.Dps * Mathf.Min(deltaTime,burn.Remaining);
            burn.Remaining -= deltaTime; if(burn.Remaining <= 0) burns.RemoveAt(i);
        }
        for(int i=slows.Count-1;i>=0;i--) {
            slows[i].Remaining -= deltaTime;
            if(slows[i].Remaining <= 0) { owner.Movement.Modifiers.Remove(slows[i].Source); slows.RemoveAt(i); }
        }
        if(damage > 0) owner.ReceiveDamage(damage);
    }
    public void Clear() {
        burns.Clear();
        if(owner != null && owner.Movement != null) foreach(var slow in slows) owner.Movement.Modifiers.Remove(slow.Source);
        slows.Clear();
    }
    private void OnDisable() => Clear();
}
}
