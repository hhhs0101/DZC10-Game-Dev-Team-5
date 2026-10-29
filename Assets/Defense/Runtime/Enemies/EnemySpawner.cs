using UnityEngine;
namespace Defense {
public sealed class EnemySpawner : MonoBehaviour {
    [SerializeField] private WaypointPath path;
    [SerializeField, Tooltip("Optional multi-branch routes; when populated, each ungrouped spawn or new group chooses a branch.")] private WaypointPath[] branchPaths;
    [SerializeField, Tooltip("Optional Stage 3 left diverging route leading off-screen before re-emerging on the right.")] private WaypointPath divergingEntryPath;
    [SerializeField, Tooltip("Optional Stage 3 right re-emergence route leading back onto the main path to the destination.")] private WaypointPath divergingExitPath;
    [SerializeField, Min(0f)] private float divergingHideSeconds = 3f;
    [SerializeField] private BaseHealth baseHealth;
    [SerializeField] private EnemyRegistry registry;
    [SerializeField] private GameFlow flow;
    [SerializeField] private Transform enemyRoot;
    [SerializeField, Tooltip("Optional native 2D actor prefab. EnemyDefinition still owns stats/rewards.")] private Enemy spritePrefab;
    [SerializeField, Tooltip("Relative weights in branchPaths order; omitted entries use equal weight.")] private float[] branchWeights;
    [SerializeField, Min(0)] private int openingPreferredBranches;
    [SerializeField, Min(0)] private int openingPreferredChoices;
    private int branchChoices;
    private int lastBranchIndex = -1;
    private int sameBranchStreak;
    private int lastDivergeChoice = -1;
    private int sameDivergeStreak;
    // Store route values, not an enemy reference: a group survives its leader's death.
    private readonly System.Collections.Generic.Dictionary<string, SpawnRoute> groupRoutes = new System.Collections.Generic.Dictionary<string, SpawnRoute>();
    private readonly System.Collections.Generic.HashSet<string> mainRouteGroups = new System.Collections.Generic.HashSet<string>();
    public void ConfigureMainRouteGroups(params string[] groups) {
        mainRouteGroups.Clear();
        if(groups != null) foreach(var group in groups)
            if(!string.IsNullOrWhiteSpace(group)) mainRouteGroups.Add(group);
        ResetSpawnGroups();
    }
    private readonly struct SpawnRoute {
        public readonly WaypointPath Path, Continuation;
        public readonly float HideSeconds;
        public SpawnRoute(WaypointPath path, WaypointPath continuation, float hideSeconds) {
            Path=path;Continuation=continuation;HideSeconds=hideSeconds;
        }
    }
    public void ResetSpawnGroups() {
        groupRoutes.Clear();
        branchChoices = 0; lastBranchIndex = -1; sameBranchStreak = 0;
    }
    public bool CanSpawn => flow != null && flow.IsPlaying;
    internal GameFlow Flow => flow;
    public void ConfigureDivergingRoute(WaypointPath entryPath, WaypointPath exitPath, float hideSeconds = 3f) {
        ResetSpawnGroups();
        divergingEntryPath = entryPath;
        divergingExitPath = exitPath;
        divergingHideSeconds = Mathf.Max(0f, hideSeconds);
        lastDivergeChoice = -1;
        sameDivergeStreak = 0;
    }
    private static bool IsBoss(EnemyDefinition definition) =>
        definition != null && (definition.Role == EnemyRole.Boss || definition.VegetableType == VegetableType.Broccoli);
    private bool ShouldUseDivergingRoute(EnemyDefinition definition) {
        if (IsBoss(definition)) return false;
        if (divergingEntryPath == null || divergingEntryPath.Count < 2 || divergingExitPath == null || divergingExitPath.Count < 2)
            return false;
        int chosen;
        if (sameDivergeStreak >= 2 && (lastDivergeChoice == 0 || lastDivergeChoice == 1)) {
            chosen = 1 - lastDivergeChoice;
        } else {
            chosen = UnityEngine.Random.Range(0, 2);
        }
        if (chosen == lastDivergeChoice) sameDivergeStreak++;
        else {
            lastDivergeChoice = chosen;
            sameDivergeStreak = 1;
        }
        return chosen == 1;
    }
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
                // Stage-specific opening preference applies to whole groups, never individual escorts.
                if(branchChoices < openingPreferredChoices && openingPreferredBranches > 0) {
                    var preferred = validIndices.FindAll(i=>i < openingPreferredBranches);
                    if(preferred.Count > 0) validIndices = preferred;
                }
                // Prevent long single-lane streaks while keeping each independent route choice unpredictable.
                if (sameBranchStreak >= 2 && validIndices.Count > 1 && validIndices.Contains(lastBranchIndex)) {
                    validIndices.Remove(lastBranchIndex);
                }
                int chosen = ChooseWeightedBranch(validIndices);
                branchChoices++;
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
    private float BranchWeight(int index) {
        if(branchWeights == null || index >= branchWeights.Length) return 1f;
        float weight = branchWeights[index];
        return float.IsNaN(weight) || float.IsInfinity(weight) ? 1f : Mathf.Max(0f,weight);
    }
    private int ChooseWeightedBranch(System.Collections.Generic.List<int> candidates) {
        float total = 0;
        foreach(int index in candidates) total += BranchWeight(index);
        if(total <= 0) return candidates[UnityEngine.Random.Range(0,candidates.Count)];
        float roll = UnityEngine.Random.value * total;
        int fallback = candidates[0];
        foreach(int index in candidates) {
            float weight = BranchWeight(index);
            if(weight <= 0) continue;
            fallback = index;
            if(roll < weight) return index;
            roll -= weight;
        }
        return fallback;
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
    // Napa uses its authored BasicEnemy prefab, even when a scene supplies a boss fallback.
    private Enemy ActorPrefab(EnemyDefinition definition) => definition.EnemyTypeId == "enemy_baechu" || definition.Prefab.GetType() != typeof(BasicEnemy)
        ? definition.Prefab : (spritePrefab != null ? spritePrefab : definition.Prefab);
    public Enemy SpawnChild(EnemyDefinition definition, Enemy source, bool canSplit) {
        if(!CanSpawn || definition == null || definition.Prefab == null || source == null || source.Movement.Route == null) return null;
        var child = Instantiate(ActorPrefab(definition),source.transform.position,Quaternion.identity,enemyRoot);
        child.Spawner = this; child.CanSplit = canSplit;
        child.Initialize(definition,source.Movement.Route,baseHealth,registry,source.Movement);
        // Children count toward Victory through the registry, but do not farm resources/Candy.
        return child;
    }
    public Enemy Spawn(EnemyDefinition definition) => Spawn(definition,null);
    public Enemy Spawn(EnemyDefinition definition, string routeGroup) {
        if (!CanSpawn) return null;
        // Bosses keep their existing main-route rule, even if a plan supplies a group.
        bool grouped = !string.IsNullOrEmpty(routeGroup) && !IsBoss(definition);
        SpawnRoute route;
        if(!grouped || !groupRoutes.TryGetValue(routeGroup,out route)) {
            bool forceMain = grouped && mainRouteGroups.Contains(routeGroup);
            bool useDiverging = !forceMain && ShouldUseDivergingRoute(definition);
            var chosenPath = useDiverging ? divergingEntryPath : (forceMain || IsBoss(definition) ? path : SelectSpawnPath());
            route = new SpawnRoute(chosenPath,useDiverging ? divergingExitPath : null,divergingHideSeconds);
        }
        var activePath = route.Path;
        if (definition == null || definition.Prefab == null || activePath == null || activePath.Count < 2) {
            Debug.LogError("EnemySpawner requires a prefab and at least two waypoints.", this); return null;
        }
        if(grouped) groupRoutes[routeGroup] = route;
        Enemy enemy = Instantiate(ActorPrefab(definition), activePath.GetPoint(0), Quaternion.identity, enemyRoot);
        enemy.Spawner = this;
        enemy.Initialize(definition, activePath, baseHealth, registry);
        if (route.Continuation != null && enemy.Movement != null)
            enemy.Movement.SetContinuation(route.Continuation, route.HideSeconds);
        enemy.Died += OnEnemyDied;
        return enemy;
    }
}
}
