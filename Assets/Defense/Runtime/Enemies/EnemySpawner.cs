using UnityEngine;
namespace Defense {
public sealed class EnemySpawner : MonoBehaviour {
    [SerializeField] private WaypointPath path;
    [SerializeField, Tooltip("Optional multi-branch routes; when populated, each spawn cycles across branches.")] private WaypointPath[] branchPaths;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private EnemyRegistry registry;
    [SerializeField] private GameFlow flow;
    [SerializeField] private Transform enemyRoot;
    [SerializeField, Tooltip("Optional native 2D actor prefab. EnemyDefinition still owns stats/rewards.")] private Enemy spritePrefab;
    private int nextBranchIndex;
    public bool CanSpawn => flow != null && flow.IsPlaying;
    internal GameFlow Flow => flow;
    private WaypointPath SelectSpawnPath() {
        if (branchPaths != null && branchPaths.Length > 0) {
            for (int i = 0; i < branchPaths.Length; i++) {
                var candidate = branchPaths[(nextBranchIndex + i) % branchPaths.Length];
                if (candidate != null && candidate.Count >= 2) {
                    nextBranchIndex = (nextBranchIndex + i + 1) % branchPaths.Length;
                    return candidate;
                }
            }
        }
        return path;
    }
    internal bool TryValidateSpawnContext(out string issue) {
        issue = null;
        if(flow == null || baseHealth == null || registry == null || path == null || path.Count < 2) {
            issue = "EnemySpawner requires GameFlow, BaseHealth, EnemyRegistry and a path with at least two waypoints.";return false;
        }
        if(spritePrefab != null && (spritePrefab.GetComponent<EnemyHealth>() == null || spritePrefab.GetComponent<EnemyPathFollower>() == null)) {
            issue = "Native enemy override prefab requires health and movement components.";return false;
        }
        return true;
    }
    // Lifecycle notification; resource policy belongs to EnemyKillRewards.
    public event System.Action<Enemy> EnemyDefeated;
    private void OnEnemyDied(Enemy enemy) {
        enemy.Died -= OnEnemyDied;
        EnemyDefeated?.Invoke(enemy);
    }
    public Enemy Spawn(EnemyDefinition definition) {
        if (!flow.IsPlaying) return null;
        var activePath = SelectSpawnPath();
        if (definition == null || definition.Prefab == null || activePath == null || activePath.Count < 2) {
            Debug.LogError("EnemySpawner requires a prefab and at least two waypoints.", this); return null;
        }
        Enemy enemy = Instantiate(spritePrefab != null ? spritePrefab : definition.Prefab, activePath.GetPoint(0), Quaternion.identity, enemyRoot);
        enemy.Initialize(definition, activePath, baseHealth, registry);
        enemy.Died += OnEnemyDied;
        return enemy;
    }
}
}
