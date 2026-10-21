using System.Collections.Generic;
namespace Defense {
// Timers live on the target. Tokens carry no reference back to a Potato or its GameObject.
public sealed class AttackDisableStatus {
    private readonly Dictionary<object,float> sources = new Dictionary<object,float>();
    private readonly List<object> keys = new List<object>();
    public bool IsDisabled => sources.Count > 0;
    public int SourceCount => sources.Count;
    public bool Apply(object source,float duration) {
        if(source == null || duration <= 0 || float.IsNaN(duration) || float.IsInfinity(duration)) return false;
        sources[source] = duration; return true;
    }
    public void Remove(object source) { if(source != null) sources.Remove(source); }
    public void Advance(float deltaTime) {
        if(deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        keys.Clear(); keys.AddRange(sources.Keys);
        foreach(var key in keys) { float remaining = sources[key]-deltaTime; if(remaining <= 0) sources.Remove(key); else sources[key] = remaining; }
        keys.Clear();
    }
    public void Clear() { sources.Clear(); keys.Clear(); }
}
}
