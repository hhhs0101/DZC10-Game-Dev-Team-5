using UnityEngine;
namespace Defense {
// Keeps economy rules separate from enemy combat and spawn execution.
public sealed class EnemyKillRewards : MonoBehaviour {
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private ResourceWallet wallet;
    private void OnEnable() { spawner.EnemyDefeated += Award; }
    private void OnDisable() { if (spawner != null) spawner.EnemyDefeated -= Award; }
    private void Award(Enemy enemy) { wallet.Add(enemy.ResourceReward); }
}
}
