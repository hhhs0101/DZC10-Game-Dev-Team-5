using System;
using UnityEngine;
namespace Defense {
public sealed class EnemyHealth : MonoBehaviour, IDamageable {
    public float Current { get; private set; }
    public float Maximum { get; private set; }
    public event Action<float, float> Changed;
    public event Action Depleted;
    public void Initialize(float maximum) { Current = Maximum = Mathf.Max(1, maximum); Changed?.Invoke(Current, Maximum); }
    public void ReceiveDamage(float amount) {
        if (Current <= 0 || amount <= 0) return;
        Current = Mathf.Max(0, Current - amount);
        Changed?.Invoke(Current, Maximum);
        if (Current == 0) Depleted?.Invoke();
    }
}
}
