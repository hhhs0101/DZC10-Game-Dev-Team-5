using System;
using UnityEngine;
namespace Defense {
public sealed class BaseHealth : MonoBehaviour, IDamageable {
    public int Current { get; private set; }
    public int Maximum { get; private set; }
    public event Action<int> Changed;
    public event Action Depleted;
    public void Initialize(int maximum) { Current = Maximum = Mathf.Max(1, maximum); Changed?.Invoke(Current); }
    public void ReceiveDamage(float amount) {
        if (Current <= 0 || amount <= 0) return;
        Current = Mathf.Max(0, Current - Mathf.CeilToInt(amount));
        Changed?.Invoke(Current);
        if (Current == 0) Depleted?.Invoke();
    }
}
}
