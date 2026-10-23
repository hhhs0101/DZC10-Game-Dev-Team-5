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
    private int lastBranchIndex = -1;
    private int sameBranchStreak;
    public bool CanSpawn => flow != null && flow.IsPlaying;
    internal GameFlow Flow => flow;
    private WaypointPath SelectSpawnPath() {
        if ((branchPaths == null || branchPaths.Length == 0)) {
            var map = UnityEngine.Object.FindFirstObjectByType<BattlefieldMap2D>();
            if (map != null && map.BranchPaths != null && map.BranchPaths.Count > 1) {
                branchPaths = new WaypointPath[map.BranchPaths.Count];
                for (int i = 0; i < map.BranchPaths.Count; i++) branchPaths[i] = map.BranchPaths[i];
            }
        }
        if (branchPaths != null && branchPaths.Length > 1) {
            var validIndices = new System.Collections.Generic.List<int>(branchPaths.Length);
            for (int i = 0; i < branchPaths.Length; i++) {
                if (branchPaths[i] != null && branchPaths[i].Count >= 2)
                    validIndices.Add(i);
            }
            if (validIndices.Count > 1) {
                // Prevent long single-lane streaks while keeping each spawn unpredictable.
                if (sameBranchStreak >= 2 && validIndices.Contains(lastBranchIndex)) {
                    validIndices.Remove(lastBranchIndex);
                }
                int chosen = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
                if (chosen == lastBranchIndex) sameBranchStreak++;
                else {
                    lastBranchIndex = chosen;
                    sameBranchStreak = 1;
                }
                return branchPaths[chosen];
            }
            if (validIndices.Count == 1) return branchPaths[validIndices[0]];
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
    private Enemy ActorPrefab(EnemyDefinition definition) => definition.Prefab.GetType() != typeof(BasicEnemy)
        ? definition.Prefab : (spritePrefab != null ? spritePrefab : definition.Prefab);
    public Enemy SpawnChild(EnemyDefinition definition, Enemy source, bool canSplit) {
        if(!CanSpawn || definition == null || definition.Prefab == null || source == null || source.Movement.Route == null) return null;
        var child = Instantiate(ActorPrefab(definition),source.transform.position,Quaternion.identity,enemyRoot);
        child.Spawner = this; child.CanSplit = canSplit;
        child.Initialize(definition,source.Movement.Route,baseHealth,registry,source.Movement);
        // Children count toward Victory through the registry, but do not farm resources/Candy.
        return child;
    }
    public Enemy Spawn(EnemyDefinition definition) {
        if (!flow.IsPlaying) return null;
        var activePath = SelectSpawnPath();
        if (definition == null || definition.Prefab == null || activePath == null || activePath.Count < 2) {
            Debug.LogError("EnemySpawner requires a prefab and at least two waypoints.", this); return null;
        }
        Enemy enemy = Instantiate(ActorPrefab(definition), activePath.GetPoint(0), Quaternion.identity, enemyRoot);
        enemy.Spawner = this;
        enemy.Initialize(definition, activePath, baseHealth, registry);
        enemy.Died += OnEnemyDied;
        return enemy;
    }
}
}
