using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// Independent source-aware channels: strongest slow times strongest boost. Base speed is never overwritten.
public sealed class MovementModifiers {
    private readonly Dictionary<object,float> slows = new Dictionary<object,float>();
    private readonly Dictionary<object,float> boosts = new Dictionary<object,float>();
    public int SourceCount => slows.Count + boosts.Count;
    public float Multiplier {
        get { float slowest = 1, fastest = 1; foreach (var slow in slows.Values) slowest = Mathf.Min(slowest,slow); foreach (var boost in boosts.Values) fastest = Mathf.Max(fastest,boost); return slowest * fastest; }
    }
    public void SetSlow(object source, float multiplier) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) throw new ArgumentOutOfRangeException(nameof(multiplier));
        slows[source] = Mathf.Clamp01(multiplier);
    }
    public void SetSpeedBoost(object source, float multiplier) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier) || multiplier < 1) throw new ArgumentOutOfRangeException(nameof(multiplier));
        boosts[source] = multiplier;
    }
    public void Remove(object source) { if (source != null) { slows.Remove(source); boosts.Remove(source); } }
    public void Clear() { slows.Clear(); boosts.Clear(); }
}
}
