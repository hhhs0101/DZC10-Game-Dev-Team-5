using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Programmatic integration validation. Never runs against the real player profile.
[InitializeOnLoad]
public static class VeggieTestStageValidation {
    private const string Key = "Defense.TestStage2.Validation";
    private static int phase, checks, startGold;
    private static double started;
    private static StageDefinition stage;
    private static GameFlow previousFlow;
    private static string deckBytes;
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    static VeggieTestStageValidation() { if (SessionState.GetBool(Key, false)) Attach(); }
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        string path = Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
        if (string.IsNullOrWhiteSpace(path) || Path.GetFullPath(path).StartsWith(Path.GetFullPath(Application.persistentDataPath) + Path.DirectorySeparatorChar)) throw new InvalidOperationException("An isolated test deck/currency profile is required.");
        SessionState.SetBool(Key + ".Background", Application.runInBackground); Application.runInBackground = true;
        EditorSceneManager.OpenScene("Assets/Defense/Scenes/MainMenu.unity");
        SessionState.SetBool(Key, true); Attach(); EditorApplication.isPlaying = true;
    }
    private static void Attach() { phase = checks = 0; started = EditorApplication.timeSinceStartup; EditorApplication.update -= Tick; EditorApplication.update += Tick; Application.logMessageReceived -= OnLog; Application.logMessageReceived += OnLog; }
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Debug.Log("TEST2 PASS: " + message); }
    private static void OnLog(string message, string trace, LogType kind) {
        if (trace.Contains("UnityEditor.Search.SearchDatabase") && !trace.Contains("Defense.")) return;
        if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert) Finish(false, message + "\n" + trace);
    }
    private static void Click(string label) { var b = UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).Single(x => x.GetComponentInChildren<UnityEngine.UI.Text>().text == label); Check(b.interactable, "Actual UI callback enabled: " + label); b.onClick.Invoke(); }
    private static void Fresh() {
        var schedule = Find<FixedIntervalSpawner>();
        Check(PlayerSession.ActiveStage == stage && Find<GameFlow>().IsPlaying && schedule.SpawnedCount == 0 && !schedule.IsFinishedSpawning, "Same Test Stage 2 starts a fresh sequence");
        Check(Find<BaseHealth>().Current == 10 && Find<ResourceWallet>().Balance == 300 && Find<EnemyRegistry>().AliveEnemyCount == 0, "Scene reload resets Base, resources and enemies");
        Check(File.ReadAllText(DeckPersistence.DefaultPath) == deckBytes, "Persistent deck/preset data is unchanged");
        schedule.enabled = false;
    }
    private static void SpawnAndVerify() {
        var schedule = Find<FixedIntervalSpawner>(); var registry = Find<EnemyRegistry>(); var seen = new List<Enemy>();
        Action<Enemy> observe = e => { seen.Add(e); e.Movement.Stop(); };
        schedule.EnemyScheduled += observe;
        try {
            Find<GameFlow>().TogglePause(); schedule.Advance(5); Check(schedule.SpawnedCount == 0, "Pause does not consume entries"); Find<GameFlow>().TogglePause();
            schedule.Advance(0); schedule.Advance(float.NaN); schedule.Advance(float.PositiveInfinity); Check(schedule.SpawnedCount == 0, "Invalid/zero time does not consume entries");
            for (int i = 0; i < 10; i++) {
                schedule.Advance(1);
                Check(seen.Count == i + 1 && seen[i].Definition == stage.SpawnSequence[i], "One scheduled spawn in exact order: " + stage.SpawnSequence[i].DisplayName);
                Check(seen[i].GetComponentInChildren<SpriteRenderer>().sprite == stage.SpawnSequence[i].Sprite, "Correct sprite on scheduled actor: " + stage.SpawnSequence[i].DisplayName);
            }
            for (int i = 0; i < 20; i++) schedule.Advance(100);
            Check(schedule.IsFinishedSpawning && schedule.SpawnedCount == 10 && seen.Count == 10 && registry.AliveEnemyCount == 10 && seen.Select(e => e.Definition).Distinct().Count() == 10, "Ten unique entries spawn once, then scheduling stops");
            Check(stage.SpawnSequence.Select(e => e.name).SequenceEqual(VeggieEnemySetup.Names.Select(n => n + "Enemy")), "Authored spawn list remains intact");
        } finally { schedule.EnemyScheduled -= observe; }
    }
    private static void Clear(int expectedGold) {
        foreach (var e in Find<EnemyRegistry>().Enemies.ToArray()) e.ReceiveDamage(10000);
        var victory = Find<StageVictoryController>(); victory.EvaluateClear();
        Check(Find<GameFlow>().State == GameplayState.Victory && victory.DefeatedCount == 10 && PlayerSession.Currency.Gold == expectedGold, "All ten combat deaths clear stage and award configured Gold once");
        victory.EvaluateClear(); Check(PlayerSession.Currency.Gold == expectedGold, "Repeated clear check cannot duplicate Gold");
    }
    private static void Tick() {
        if (!SessionState.GetBool(Key, false)) return;
        try {
            if (EditorApplication.timeSinceStartup - started > 90) throw new Exception("Test Stage 2 validation timed out at phase " + phase);
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
            if (phase == 0) { if (GameObject.Find("Start Game") == null) return; Click("Start Game"); phase = 1; }
            else if (phase == 1) {
                var lobby = Find<LobbyUI>(); if (lobby == null) return;
                stage = PlayerSession.Catalog.Stages.Single(s => s.DisplayName == "Test Stage 2");
                Check(PlayerSession.Catalog.Stages.Count == 12 && stage.AlwaysUnlocked && stage.FiniteSpawning && stage.TotalEnemies == 10 && stage.SpawnSequence.Distinct().Count() == 10, "Catalog exposes a separate unlocked ten-entry Test Stage 2");
                var old = PlayerSession.Catalog.Stages.Single(s => s.DisplayName == "Test Stage"); Check(old.TotalEnemies == 5 && old.SpawnSequence.Count == 0 && old.ClearGoldReward == 50, "Original five-enemy Test Stage unchanged");
                while (PlayerSession.Selection.SelectedStageIndex < PlayerSession.Catalog.Stages.ToList().IndexOf(stage)) PlayerSession.Selection.MoveNext();
                lobby.Carousel.Advance(1); Check(PlayerSession.Progression.IsUnlocked(PlayerSession.Selection.SelectedStageIndex), "New stage is selectable without campaign clears");
                deckBytes = File.ReadAllText(DeckPersistence.DefaultPath); startGold = PlayerSession.Currency.Gold; Click("Enter Stage"); phase = 2;
            } else if (phase == 2) {
                if (Find<GameplayUI>() == null || GameObject.Find("Hand Card 1") == null) return;
                Fresh(); SpawnAndVerify(); Clear(startGold + stage.ClearGoldReward); previousFlow = Find<GameFlow>(); Click("Replay"); phase = 3;
            } else if (phase == 3) {
                if (Find<GameFlow>() == previousFlow || GameObject.Find("Hand Card 1") == null) return;
                Fresh(); Find<FixedIntervalSpawner>().Advance(1);
                Check(Find<EnemyRegistry>().Enemies.Single().Definition == stage.SpawnSequence[0], "Replay starts from Mushroom, not a consumed queue position");
                Find<GameFlow>().GameOver(); previousFlow = Find<GameFlow>(); Click("Retry"); phase = 4;
            } else if (phase == 4) {
                if (Find<GameFlow>() == previousFlow || GameObject.Find("Hand Card 1") == null) return;
                Fresh(); SpawnAndVerify(); Clear(startGold + stage.ClearGoldReward * 2); Click("Return to Lobby"); phase = 5;
            } else if (phase == 5) {
                if (Find<LobbyUI>() == null) return;
                Check(PlayerSession.Catalog.Stages[PlayerSession.Selection.SelectedStageIndex] == stage && File.ReadAllText(DeckPersistence.DefaultPath) == deckBytes, "Return to Lobby keeps selected stage and persistent deck");
                Check(JsonUtility.FromJson<PlayerCurrencySaveData>(File.ReadAllText(PlayerSession.Currency.SavePath)).Gold == startGold + stage.ClearGoldReward * 2, "Repeat clear rewards saved in isolated currency profile");
                Finish(true, checks + " assertions");
            }
        } catch (Exception e) { Finish(false, e.ToString()); }
    }
    private static void Finish(bool pass, string message) {
        if (!SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false); EditorApplication.update -= Tick; Application.logMessageReceived -= OnLog;
        Directory.CreateDirectory("Documentation/Report"); File.WriteAllText("Documentation/Report/Test_Stage_2_Result.txt", (pass ? "PASS: " : "FAIL: ") + message);
        if (pass) Debug.Log("TEST_STAGE_2_PASSED " + message); else Debug.LogError("TEST_STAGE_2_FAILED " + message);
        EditorApplication.isPlaying = false; Application.runInBackground = SessionState.GetBool(Key + ".Background", false);
        if (Application.isBatchMode) EditorApplication.Exit(pass ? 0 : 1);
    }
}
}
