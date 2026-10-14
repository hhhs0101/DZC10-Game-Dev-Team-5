using System;
using System.Collections.Generic;
using UnityEngine;
namespace Defense {
// Source ownership is independent of the stacking policy. Current policy: strongest slow wins.
public sealed class MovementModifiers {
    private readonly Dictionary<object,float> slows = new Dictionary<object,float>();
    public int SourceCount => slows.Count;
    public float Multiplier {
        get { float value = 1; foreach (var slow in slows.Values) value = Mathf.Min(value,slow); return value; }
    }
    public void SetSlow(object source, float multiplier) {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (float.IsNaN(multiplier) || float.IsInfinity(multiplier)) throw new ArgumentOutOfRangeException(nameof(multiplier));
        slows[source] = Mathf.Clamp01(multiplier);
    }
    public void Remove(object source) { if (source != null) slows.Remove(source); }
    public void Clear() => slows.Clear();
}
}
