using System;
using UnityEngine;
namespace Defense {
public sealed class EnemyHealth : MonoBehaviour, IDamageable {
    public int LayerCount { get; private set; } = 1;
    public float LayerMaximum => Maximum / LayerCount;
    public float Current { get; private set; }
    public float Maximum { get; private set; }
    private float[] layerHealth = new float[0];
    public event Action<float, float> Changed;
    public event Action Depleted;
    public float GetLayerHealth(int index) => index >= 0 && index < layerHealth.Length ? layerHealth[index] : 0;
    public void Initialize(float maximum, int layers = 1) {
        LayerCount = Mathf.Max(1,layers);
        Current = Maximum = Mathf.Max(1,maximum) * LayerCount;
        layerHealth = new float[LayerCount];
        for (int i = 0; i < LayerCount; i++) layerHealth[i] = LayerMaximum;
        Changed?.Invoke(Current,Maximum);
    }
    // The moving Corn body mirrors its child targets for existing UI observers.
    internal void SetLayerHealth(int index, float value) {
        if(index < 0 || index >= layerHealth.Length) return;
        float previous = Current;
        layerHealth[index] = Mathf.Clamp(value,0,LayerMaximum);
        Current = 0;
        for(int i = 0; i < layerHealth.Length; i++) Current += layerHealth[i];
        Changed?.Invoke(Current,Maximum);
        if(previous > 0 && Current == 0) Depleted?.Invoke();
    }
    public void ReceiveDamage(float amount) => ApplyDamage(amount, false);
    public void ReceiveSplashDamage(float amount) => ApplyDamage(amount, true);
    private void ApplyDamage(float amount, bool splash) {
        if (Current <= 0 || amount <= 0 || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        for (int i = 0; i < layerHealth.Length; i++) {
            if (layerHealth[i] <= 0) continue;
            // Direct hits stop at one target. Splash hits every surviving target once.
            layerHealth[i] = Mathf.Max(0, layerHealth[i] - amount);
            if (!splash) break;
        }
        Current = 0;
        for (int i = 0; i < layerHealth.Length; i++) Current += layerHealth[i];
        Changed?.Invoke(Current, Maximum);
        if (Current == 0) Depleted?.Invoke();
    }
}
}
