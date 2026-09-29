using UnityEngine;
namespace Defense {
public sealed class EnemySpawner : MonoBehaviour {
    [SerializeField] private WaypointPath path;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private EnemyRegistry registry;
    [SerializeField] private GameFlow flow;
    [SerializeField] private Transform enemyRoot;
    // Lifecycle notification; resource policy belongs to EnemyKillRewards.
    public event System.Action<Enemy> EnemyDefeated;
    private void OnEnemyDied(Enemy enemy) {
        enemy.Died -= OnEnemyDied;
        EnemyDefeated?.Invoke(enemy);
    }
    public Enemy Spawn(EnemyDefinition definition) {
        if (!flow.IsPlaying) return null;
        if (definition == null || definition.Prefab == null || path == null || path.Count < 2) {
            Debug.LogError("EnemySpawner requires a prefab and at least two waypoints.", this); return null;
        }
        Enemy enemy = Instantiate(definition.Prefab, path.GetPoint(0), Quaternion.identity, enemyRoot);
        enemy.Initialize(definition, path, baseHealth, registry);
        enemy.Died += OnEnemyDied;
        return enemy;
    }
}
}
