using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Defense.Editor {
// No extra test package required. Runs assertions against the generated scenes in Play Mode.
[InitializeOnLoad]
public static class PrototypeValidation {
    private const string Key = "Defense.Validation.Running";
    private static int step;
    private static double deadline;
    private static double started;
    private static Enemy pausedEnemy, combatEnemy;
    private static Vector3 pausedPosition;
    private static float pausedHealth;
    private static int pausedCount;
    private static string lastMessage;
    private static int assertions;
    private static int balanceBeforeBase, balanceBeforeCombat;
    private static int turretIndex;
    private static Turret combatTurret;
    private static EnemyDefinition durableEnemy;
    private static readonly System.Collections.Generic.List<float> hitTimes = new System.Collections.Generic.List<float>();
    private static readonly System.Collections.Generic.List<float> hitHealth = new System.Collections.Generic.List<float>();
    static PrototypeValidation() {
        if (SessionState.GetBool(Key,false)) Attach();
    }
    [MenuItem("Defense/Run Prototype Validation (Play Mode)")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before validation.");
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");
        Attach(); EditorApplication.isPlaying = true;
    }
    private static void Attach() {
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog;
    }
    private static void OnLog(string message, string trace, LogType type) {
        // Unity 6000.6 can throw during its own asynchronous Search index startup in batch mode.
        // Exclude only that Editor-only stack; all runtime/game exceptions still fail validation.
        if (trace.Contains("UnityEditor.Search.SearchDatabase") && !trace.Contains("Defense.")) {
            Debug.LogWarning("Editor Search indexing issue (outside gameplay): " + message); return;
        }
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Finish(false,message + "\n" + trace);
    }
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Check(bool condition, string label) {
        if (!condition) throw new Exception(label);
        assertions++; Debug.Log("PASS: " + label);
    }
    private static void Click(string label) {
        Button button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.GetComponentInChildren<Text>().text == label);
        Check(button.interactable,"Button available: " + label); button.onClick.Invoke();
    }
    private static void Wait(int next, double delay = .25) { step = next; deadline = EditorApplication.timeSinceStartup + delay; }
    private static void Tick() {
        if (!SessionState.GetBool(Key,false)) return;
        try {
            if (EditorApplication.timeSinceStartup - started > 60) throw new Exception("Play Mode validation timed out.");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < deadline) return;
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Defense/Data/TestStage.asset");
            switch (step) {
                case 0:
                    if (Find<MainMenuUI>() == null || Find<Button>() == null) return;
                    Click("Settings"); Click("Back"); Click("Start Game"); Click("Test Stage"); Wait(1); break;
                case 1:
                    if (Find<EnemyRegistry>() == null || Find<EnemyRegistry>().Enemies.Count == 0) return;
                    Check(SceneManager.GetActiveScene().name == "TestStage","Stage catalog loads TestStage");
                    Check(Find<ResourceWallet>().Balance == 150,"Starting resources");
                    Check(Find<BaseHealth>().Current == 10,"Starting Base HP");
                    Check(Find<EnemyRegistry>().Enemies.Count > 0,"Fixed interval actually spawns enemies");
                    Find<FixedIntervalSpawner>().enabled = false;
                    TestBuildMenu(stage);
                    TestPlacementAndTargeting(stage);
                    TestKillRewards(stage);
                    balanceBeforeBase = Find<ResourceWallet>().Balance;
                    pausedEnemy = Find<EnemySpawner>().Spawn(stage.Enemy);
                    Click("Pause");
                    Check(Find<GameFlow>().State == GameplayState.Paused && Time.timeScale == 0,"Pause button freezes gameplay");
                    pausedPosition = pausedEnemy.transform.position; pausedHealth = pausedEnemy.CurrentHealth;
                    pausedCount = Find<EnemyRegistry>().Enemies.Count;
                    Check(!Find<TurretPlacementController>().TryPlace(Find<TowerPlacementSlot>()),"Placement blocked during pause");
                    Check(Find<EnemySpawner>().Spawn(stage.Enemy) == null,"Spawning blocked during pause");
                    Click("Settings"); Click("Back");
                    Check(Find<GameFlow>().State == GameplayState.Paused,"Settings Back preserves pause");
                    Wait(2,.5); break;
                case 2:
                    Check(pausedEnemy.transform.position == pausedPosition,"Enemy position stays frozen");
                    Check(pausedEnemy.CurrentHealth == pausedHealth,"Turrets do not damage while paused");
                    Check(Find<EnemyRegistry>().Enemies.Count == pausedCount,"Enemy count stays frozen");
                    Click("Resume"); Check(Time.timeScale == 1,"Resume restores time");
                    pausedEnemy.GetComponent<EnemyPathFollower>().Advance(100);
                    Check(Find<BaseHealth>().Current == 9,"Reaching Base applies damage");
                    pausedEnemy.GetComponent<EnemyPathFollower>().Advance(100);
                    Check(Find<BaseHealth>().Current == 9,"Reaching Base only applies damage once");
                    Check(Find<ResourceWallet>().Balance == balanceBeforeBase,"Reaching Base grants no resources");
                    balanceBeforeCombat = Find<ResourceWallet>().Balance;
                    combatEnemy = Find<EnemySpawner>().Spawn(stage.Enemy);
                    combatEnemy.GetComponent<EnemyPathFollower>().Stop();
                    var tower = Find<Turret>(); combatEnemy.transform.position = tower.transform.position + Vector3.right*.2f;
                    Wait(3,1); break;
                case 3:
                    Check(combatEnemy == null || combatEnemy.CurrentHealth < stage.Enemy.Health,"Turret auto attack applies damage");
                    Wait(4,3.2); break;
                case 4:
                    Check(combatEnemy == null,"Combat removes enemy at zero HP");
                    Check(Find<ResourceWallet>().Balance == balanceBeforeCombat + stage.Enemy.ResourceReward,"Turret kill credits configured reward");
                    Find<BaseHealth>().ReceiveDamage(100);
                    Check(Find<GameFlow>().State == GameplayState.GameOver && Time.timeScale == 0,"Base depletion enters Game Over");
                    Check(Find<EnemySpawner>().Spawn(stage.Enemy) == null,"Game Over prevents spawning");
                    Check(!Find<TurretPlacementController>().TryPlace(Find<TowerPlacementSlot>()),"Game Over prevents placement");
                    Find<GameFlow>().TogglePause(); Check(Find<GameFlow>().State == GameplayState.GameOver,"Game Over cannot resume");
                    Click("Main Menu"); Wait(5); break;
                case 5:
                    Check(Time.timeScale == 1,"Leaving Game Over resets time");
                    Click("Start Game"); Click("Test Stage"); Wait(6); break;
                case 6:
                    Check(Find<ResourceWallet>().Balance == 150 && Find<BaseHealth>().Current == 10,"Reentering stage resets HP/resources");
                    Check(UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None).All(s => !s.IsOccupied),"Reentering stage resets slots");
                    Click("Pause"); Click("Exit"); Click("No");
                    Check(Find<GameFlow>().State == GameplayState.Paused,"Exit No returns to paused state");
                    Click("Exit"); Click("Yes"); Wait(7); break;
                case 7:
                    Check(SceneManager.GetActiveScene().name == "MainMenu" && Time.timeScale == 1,"Exit Yes returns to Main Menu");
                    Click("Start Game"); Click("Test Stage"); Wait(8); break;
                case 8:
                    Find<FixedIntervalSpawner>().enabled = false;
                    foreach (var enemy in Find<EnemyRegistry>().Enemies.ToArray()) enemy.ReceiveDamage(10000);
                    Find<ResourceWallet>().Initialize(1000);
                    BeginTurretCombat(stage); Wait(9,.3); break;
                case 9:
                    Check(combatEnemy.CurrentHealth == 1000,"Turret " + (turretIndex+1) + " excludes enemy outside its own range");
                    combatEnemy.transform.position = combatTurret.transform.position + Vector3.right*(stage.AvailableTurrets[turretIndex].AttackRange-.1f);
                    Wait(10,.01); break;
                case 10:
                    if (hitTimes.Count < 3) return;
                    var data = stage.AvailableTurrets[turretIndex];
                    Check(Mathf.Approximately(hitHealth[0],1000-data.Damage) && Mathf.Approximately(hitHealth[1],1000-2*data.Damage),"Turret " + (turretIndex+1) + " applies its own damage");
                    Check(hitTimes[1]-hitTimes[0] >= data.AttackCooldown-.03f && hitTimes[2]-hitTimes[1] >= data.AttackCooldown-.03f,"Turret " + (turretIndex+1) + " respects its own cooldown");
                    combatTurret.enabled = false;
                    combatEnemy.ReceiveDamage(10000);
                    UnityEngine.Object.Destroy(durableEnemy);
                    turretIndex++;
                    if (turretIndex < stage.AvailableTurrets.Count) { BeginTurretCombat(stage); Wait(9,.3); }
                    else Finish(true,"Assertions: " + assertions);
                    break;
            }
        } catch (Exception exception) { Finish(false,exception.ToString()); }
    }
    private static void TestBuildMenu(StageDefinition stage) {
        var menu = Find<BuildMenuUI>();
        var placement = Find<TurretPlacementController>();
        Check(!menu.IsOpen && placement.Selected == null,"Build options hidden by default with no selected type");
        Check(!UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Any(b => b.name.Contains("Basic Turret")),"Old always-visible Basic Turret button removed");
        Click("Turrets"); Check(menu.IsOpen,"Turrets button opens panel");
        Click("Turrets"); Check(!menu.IsOpen,"Turrets button toggles panel closed");
        Check(stage.AvailableTurrets.Count == 3,"Exactly three test turret definitions available");
        foreach (var definition in stage.AvailableTurrets) {
            Click("Turrets"); Click(definition.DisplayName + " (" + definition.Cost + ")");
            Check(placement.Selected == definition && !menu.IsOpen,"Selection passes definition and closes panel");
            Check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == "Selected: " + definition.DisplayName + " (" + definition.Cost + ")"),"Selected turret remains labelled with panel closed");
        }
        Check(Find<ResourceWallet>().Balance == 150,"UI selection does not place or spend resources");
    }
    private static void BeginTurretCombat(StageDefinition stage) {
        var data = stage.AvailableTurrets[turretIndex];
        Click("Turrets"); Click(data.DisplayName + " (" + data.Cost + ")");
        var slot = UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None).OrderBy(s => s.name).First(s => !s.IsOccupied);
        int before = Find<ResourceWallet>().Balance;
        Check(Find<TurretPlacementController>().TryPlace(slot) && Find<ResourceWallet>().Balance == before-data.Cost,"Selected turret " + (turretIndex+1) + " spends its own cost");
        combatTurret = slot.Occupant;
        durableEnemy = UnityEngine.Object.Instantiate(stage.Enemy);
        var serialized = new SerializedObject(durableEnemy); serialized.FindProperty("health").floatValue = 1000; serialized.ApplyModifiedPropertiesWithoutUndo();
        combatEnemy = Find<EnemySpawner>().Spawn(durableEnemy); combatEnemy.GetComponent<EnemyPathFollower>().Stop();
        combatEnemy.transform.position = combatTurret.transform.position + Vector3.right*(data.AttackRange+.1f);
        hitTimes.Clear(); hitHealth.Clear();
        combatEnemy.GetComponent<EnemyHealth>().Changed += (hp,max) => { if (hp > 0) { hitTimes.Add(Time.time); hitHealth.Add(hp); } };
    }
    private static void TestPlacementAndTargeting(StageDefinition stage) {
        var placement = Find<TurretPlacementController>(); var wallet = Find<ResourceWallet>();
        placement.Message += message => lastMessage = message;
        placement.Select(stage.AvailableTurrets[0]);
        Check(!placement.TryPlace(null) && lastMessage == "You cannot place a turret here." && wallet.Balance == 150,"Invalid location consumes no resources");
        var slots = UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None).OrderBy(s => s.name).ToArray();
        Color available = slots[0].GetComponent<SpriteRenderer>().color;
        slots[0].SetHovered(true);
        Check(slots[0].GetComponent<SpriteRenderer>().color != available,"Available slot highlights on hover");
        slots[0].SetHovered(false);
        Check(slots[0].GetComponent<SpriteRenderer>().color == available,"Hover exit restores available appearance");
        Check(placement.TryPlace(slots[0]) && wallet.Balance == 100,"Successful placement spends exact cost");
        Color occupied = slots[0].GetComponent<SpriteRenderer>().color;
        Check(occupied != available,"Occupied slot has distinct appearance");
        slots[0].SetHovered(true);
        Check(slots[0].GetComponent<SpriteRenderer>().color == occupied,"Occupied hover preserves occupied appearance");
        Check(!placement.TryPlace(slots[0]) && lastMessage == "A turret is already placed here." && wallet.Balance == 100,"Occupied slot consumes no resources");
        Check(placement.TryPlace(slots[1]) && placement.TryPlace(slots[2]) && wallet.Balance == 0,"Exact balance can be spent");
        Check(!placement.TryPlace(slots[3]) && lastMessage == "Not enough resources." && !slots[3].IsOccupied && wallet.Balance == 0,"Insufficient resources leave slot and balance intact");
        var registry = Find<EnemyRegistry>();
        foreach (var enemy in registry.Enemies.ToArray()) enemy.ReceiveDamage(1000);
        Enemy near = Find<EnemySpawner>().Spawn(stage.Enemy), far = Find<EnemySpawner>().Spawn(stage.Enemy);
        near.transform.position = Vector3.right; far.transform.position = Vector3.right*2;
        var strategy = stage.AvailableTurrets[0].Targeting;
        Check(strategy.Select(Vector3.zero,3,registry.Enemies) == near,"Closest selects physical nearest");
        near.transform.position = Vector3.right*4;
        Check(strategy.Select(Vector3.zero,3,registry.Enemies) == far,"Out of range target is reacquired");
        far.ReceiveDamage(1000);
        Check(!far.IsAlive && !registry.Enemies.Contains(far),"Death immediately removes target from registry");
        Check(strategy.Select(Vector3.zero,3,registry.Enemies) == null,"Dead and out of range enemies are excluded");
        near.ReceiveDamage(1000);
    }
    private static void TestKillRewards(StageDefinition stage) {
        var wallet = Find<ResourceWallet>();
        var spawner = Find<EnemySpawner>();
        foreach (int reward in new[] { 7, 23, 0 }) {
            var definition = UnityEngine.Object.Instantiate(stage.Enemy);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("resourceReward").intValue = reward;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            int before = wallet.Balance;
            Enemy enemy = spawner.Spawn(definition);
            var bar = enemy.GetComponent<EnemyHealthBar>();
            Check(bar.IsVisible && Mathf.Approximately(bar.NormalizedHealth,1),"Enemy HP bar starts full");
            enemy.ReceiveDamage(1);
            Check(Mathf.Approximately(bar.NormalizedHealth,(enemy.MaximumHealth-1)/enemy.MaximumHealth),"Enemy HP bar updates on nonlethal damage");
            Check(wallet.Balance == before,"Nonlethal damage grants no reward");
            enemy.ReceiveDamage(1000);
            Check(wallet.Balance == before + reward,"Per-definition reward credited: " + reward);
            Check(!bar.IsVisible && bar.NormalizedHealth == 0,"Enemy HP bar hides immediately on death");
            enemy.ReceiveDamage(1000);
            Check(wallet.Balance == before + reward,"Repeated damage cannot duplicate reward");
            UnityEngine.Object.Destroy(definition);
        }
        int balance = wallet.Balance;
        Enemy removed = spawner.Spawn(stage.Enemy);
        removed.gameObject.SetActive(false);
        UnityEngine.Object.Destroy(removed.gameObject);
        Check(wallet.Balance == balance,"Disabling/removing an enemy grants no reward");
        Check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t => t.text == "Resources: " + balance),"Resource HUD reflects kill rewards");
        wallet.Add(0); wallet.Add(-10);
        Check(wallet.Balance == balance,"Zero/negative credit cannot lower balance");
    }
    private static void Finish(bool success, string detail) {
        SessionState.SetBool(Key,false); EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        string result = (success ? "DEFENSE_VALIDATION_PASSED " : "DEFENSE_VALIDATION_FAILED ") + detail;
        Debug.Log(result);
        System.IO.File.WriteAllText("Documentation/ValidationResult.txt", result + "\n");
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
}
