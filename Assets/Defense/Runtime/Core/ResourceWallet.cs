using System;
using UnityEngine;
namespace Defense {
public sealed class ResourceWallet : MonoBehaviour {
    public int Balance { get; private set; }
    public event Action<int> Changed;
    public void Initialize(int amount) { Balance = Mathf.Max(0, amount); Changed?.Invoke(Balance); }
    public void Add(int amount) {
        if (amount <= 0) return;
        // Saturate instead of overflowing into a negative balance.
        Balance = (int)Math.Min(int.MaxValue, (long)Balance + amount);
        Changed?.Invoke(Balance);
    }
    public bool CanAfford(int cost) => cost >= 0 && Balance >= cost;
    public bool TrySpend(int cost) {
        if (!CanAfford(cost)) return false;
        Balance -= cost;
        Changed?.Invoke(Balance);
        return true;
    }
}
}
