using System;
using UnityEngine;
namespace Defense {
// Uses the spawner's once-only combat death event; removal/Base arrival is never a drop.
public sealed class CandyDropSystem : MonoBehaviour {
    private EnemySpawner spawner;
    private PlayerCurrency currency;
    private Func<double> roll;
    private bool subscribed;
    public void Initialize(EnemySpawner source, PlayerCurrency owner, Func<double> randomRoll = null) {
        Unsubscribe(); spawner = source; currency = owner;
        var random = new System.Random(); roll = randomRoll ?? random.NextDouble;
        Subscribe();
    }
    private void Award(Enemy enemy) {
        var data = enemy.Definition;
        double sample = roll(); // Exactly one sample per valid death, including 0%/100% cases.
        if (sample < data.CandyDropChance && !currency.AddCandy(data.CandyDropAmount))
            Debug.LogWarning("Candy could not be credited because the currency limit was reached.");
    }
    private void Subscribe() {
        if (subscribed || spawner == null || !isActiveAndEnabled) return;
        spawner.EnemyDefeated += Award; subscribed = true;
    }
    private void Unsubscribe() { if (subscribed && spawner != null) spawner.EnemyDefeated -= Award; subscribed = false; }
    private void OnEnable() => Subscribe();
    private void OnDisable() => Unsubscribe();
}
}
