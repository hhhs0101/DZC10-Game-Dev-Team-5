using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace Defense.Editor {
// Invoked by the existing Play Mode regression runner. Saves are isolated from player data.
public static class RewardValidation {
    private static int startGold, startCandy, failureCandy, progressionChanges;
    private static RuntimeCardCycle previousCycle;
    private static CardDefinition[] deck;
    private static StageDefinition stage;
    private static T Find<T>() where T:UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    private static PlayerCurrency Reload(string path) => new PlayerCurrency(new JsonFileStore<PlayerCurrencySaveData>(path,"Test currency"));
    public static void Models(Action<bool,string> check) {
        string folder=Path.Combine(Path.GetTempPath(),"defense-currency-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var errors=new List<string>(); var warnings=new List<string>();
        var store=new JsonFileStore<PlayerCurrencySaveData>(Path.Combine(folder,"currency.json"),"Currency",warnings.Add,errors.Add);
        Func<PlayerCurrency> load=()=>new PlayerCurrency(store,warnings.Add);
        try {
            var currency=load(); int changes=0; currency.Changed+=()=>changes++;
            check(currency.Gold==0 && currency.Candy==0 && File.Exists(store.SavePath),"Currency starts at zero and saves defaults");
            check(currency.AddGold(50) && currency.AddCandy(7) && changes==2 && store.Load().Gold==50 && store.Load().Candy==7,"Currency changes notify and persist immediately");
            string before=File.ReadAllText(store.SavePath);
            check(!currency.AddGold(-1) && !currency.AddCandy(-1) && !currency.TrySpendGold(51) && !currency.TrySpendCandy(8) && !currency.TrySpendGold(-1) && !currency.TrySpendCandy(-1) && File.ReadAllText(store.SavePath)==before && changes==2,"Invalid currency changes preserve balances, file and notifications");
            check(currency.TrySpendGold(20) && currency.TrySpendCandy(2) && load().Gold==30 && load().Candy==5,"Spending saves both balances for fresh model restore");
            before=File.ReadAllText(store.SavePath); Directory.CreateDirectory(store.SavePath+".tmp");
            check(currency.AddCandy(1) && currency.Candy==6 && !currency.LastSaveSucceeded && File.ReadAllText(store.SavePath)==before && errors.Any(e=>e.Contains("NOT saved")),"Failed save keeps accepted in-memory Candy and intact prior file with explicit error");
            Directory.Delete(store.SavePath+".tmp");
            check(currency.AddGold(1) && currency.LastSaveSucceeded && load().Candy==6,"Next successful mutation persists complete state after a failed save");
            check(currency.AddGold(int.MaxValue-currency.Gold) && !currency.AddGold(1) && currency.Gold==int.MaxValue,"Gold overflow cannot wrap balances");
            check(currency.AddCandy(int.MaxValue-currency.Candy) && !currency.AddCandy(1) && currency.Candy==int.MaxValue,"Candy overflow cannot wrap balances");
            store.Save(new PlayerCurrencySaveData {Version=1,Gold=-5,Candy=-3}); currency=load();
            check(currency.Gold==0 && currency.Candy==0 && store.Load().Gold==0,"Negative saved balances repair and persist safely");
            foreach(string json in new[]{"invalid json","null","{}","{\"Version\":99}"}) {
                File.WriteAllText(store.SavePath,json); currency=load();
                check(currency.Gold==0 && currency.Candy==0 && store.Load().Version==1,"Invalid currency save recovers: "+json);
            }
        } finally { Directory.Delete(folder,true); }
    }
    public static void Enter(Action<bool,string> check) {
        stage=PlayerSession.Catalog.Stages.Last();
        check(stage.DisplayName=="Test Stage" && stage.FiniteSpawning && stage.TotalEnemies==5 && stage.ClearGoldReward==50 && stage.Enemy.CandyDropChance==1,"Dedicated test stage defines five finite enemies, 50 Gold and guaranteed Candy");
        for(int i=0;i<PlayerSession.Catalog.Stages.Count;i++) PlayerSession.Selection.Move(1);
        check(PlayerSession.TrySelectStageForPlay(),"Test Stage can be selected through existing stage model without campaign unlocks");
        startGold=PlayerSession.Currency.Gold; startCandy=PlayerSession.Currency.Candy;
        deck=PlayerSession.Deck.Slots.ToArray();
        PlayerSession.Progression.Changed+=OnProgression;
        SceneManager.LoadScene(stage.SceneName);
    }
    private static void OnProgression() { progressionChanges++; }
    private static void StopSchedule() { Find<FixedIntervalSpawner>().enabled=false; }
    private static void FillSchedule(Action<bool,string> check) {
        var schedule=Find<FixedIntervalSpawner>();
        for(int i=0;i<8;i++) schedule.Advance(stage.SpawnInterval);
        check(schedule.SpawnedCount==5 && schedule.IsFinishedSpawning,"Finite schedule spawns exactly five and rejects a sixth");
    }
    private static void KillAll() { foreach(var enemy in Find<EnemyRegistry>().Enemies.ToArray()) enemy.ReceiveDamage(100000); }
    public static void ClearFirst(Action<bool,string> check) {
        StopSchedule(); var victory=Find<StageVictoryController>();
        check(!victory.EvaluateClear(),"Empty population cannot clear before finite spawning finishes");
        // Deterministic rolls against real spawner/Enemy death events and per-definition data.
        int rolls=0; double sample=.75;
        var drops=Find<CandyDropSystem>(); drops.Initialize(Find<EnemySpawner>(),PlayerSession.Currency,()=>{rolls++;return sample;});
        var definition=UnityEngine.Object.Instantiate(stage.Enemy);
        var data=new SerializedObject(definition);
        data.FindProperty("candyDropChance").floatValue=.5f; data.FindProperty("candyDropAmount").intValue=3; data.ApplyModifiedPropertiesWithoutUndo();
        var enemy=Find<EnemySpawner>().Spawn(definition); enemy.ReceiveDamage(10000); enemy.ReceiveDamage(10000);
        check(rolls==1 && PlayerSession.Currency.Candy==startCandy,"Failed 50% drop rolls exactly once despite repeated lethal damage");
        sample=.25; enemy=Find<EnemySpawner>().Spawn(definition); enemy.ReceiveDamage(10000);
        check(rolls==2 && PlayerSession.Currency.Candy==startCandy+3 && Reload(PlayerSession.Currency.SavePath).Candy==startCandy+3,"Successful per-enemy amount is immediately owned and saved without input");
        data.FindProperty("candyDropChance").floatValue=-1; data.FindProperty("candyDropAmount").intValue=-2; data.ApplyModifiedPropertiesWithoutUndo();
        check(definition.CandyDropChance==0 && definition.CandyDropAmount==0,"Enemy configuration safely clamps negative chance and amount");
        enemy=Find<EnemySpawner>().Spawn(definition); enemy.ReceiveDamage(10000);
        check(rolls==3 && PlayerSession.Currency.Candy==startCandy+3,"Zero-chance enemy performs one roll and awards nothing");
        data.FindProperty("candyDropChance").floatValue=2; data.ApplyModifiedPropertiesWithoutUndo();
        check(definition.CandyDropChance==1,"Enemy probability caps at one");
        enemy=Find<EnemySpawner>().Spawn(definition); UnityEngine.Object.DestroyImmediate(enemy.gameObject);
        check(rolls==3,"Arbitrary destruction is not a Candy-producing death");
        UnityEngine.Object.DestroyImmediate(definition);
        startCandy+=3;
        FillSchedule(check);
        check(Find<EnemyRegistry>().AliveEnemyCount==5 && !victory.EvaluateClear(),"Victory waits while configured enemies remain alive");
        var enemies=Find<EnemyRegistry>().Enemies.ToArray();
        for(int i=0;i<4;i++) enemies[i].ReceiveDamage(10000);
        check(!victory.EvaluateClear() && PlayerSession.Currency.Gold==startGold,"Four kills do not award Gold while final enemy remains");
        Find<GameFlow>().TogglePause(); enemies[4].ReceiveDamage(10000);
        check(!victory.EvaluateClear() && PlayerSession.Currency.Gold==startGold,"Paused clear condition does not commit Victory or Gold");
        Find<GameFlow>().TogglePause(); // LateUpdate performs the actual victory transition.
        previousCycle=Find<GameplayHand>().Cycle;
    }
    public static void VerifyVictory(Action<bool,string> check,int run) {
        check(Find<GameFlow>().State==GameplayState.Victory && Time.timeScale==0,"Actual LateUpdate enters terminal frozen Victory");
        check(PlayerSession.Currency.Gold==startGold+50*run && PlayerSession.Currency.Candy==startCandy+5*run,"Each clear grants 50 Gold and keeps five automatic Candy drops");
        check(Reload(PlayerSession.Currency.SavePath).Gold==PlayerSession.Currency.Gold && Reload(PlayerSession.Currency.SavePath).Candy==PlayerSession.Currency.Candy,"Victory balances are already saved to disk");
        check(progressionChanges==1 && PlayerSession.Progression.IsComplete(PlayerSession.Catalog.Stages.Count-1),"First completion updates progression once; repeat is idempotent");
        Find<StageVictoryController>().EvaluateClear(); Find<GameFlow>().GameOver(); Find<GameFlow>().TogglePause();
        check(PlayerSession.Currency.Gold==startGold+50*run && Find<GameFlow>().State==GameplayState.Victory,"Repeated evaluation, Game Over and pause cannot alter committed Victory");
        check(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Stage Clear") && UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Gold Earned: 50"),"Victory UI presents Stage Clear and actual configured reward");
        previousCycle=Find<GameplayHand>().Cycle;
    }
    public static void VerifyReplay(Action<bool,string> check) {
        StopSchedule();
        check(PlayerSession.ActiveStage==stage && Find<GameFlow>().IsPlaying && Find<StageVictoryController>().GoldEarned==0 && Find<FixedIntervalSpawner>().SpawnedCount==0 && Find<EnemyRegistry>().AliveEnemyCount==0,"Replay reloads same stage with reset terminal, reward, spawn and population state");
        check(!ReferenceEquals(previousCycle,Find<GameplayHand>().Cycle) && PlayerSession.Deck.Slots.SequenceEqual(deck),"Replay creates a fresh card cycle without editing persistent deck");
        check(Find<BaseHealth>().Current==stage.BaseHealth && Find<ResourceWallet>().Balance==stage.StartingResources && UnityEngine.Object.FindObjectsByType<Turret>(FindObjectsSortMode.None).Length==0 && UnityEngine.Object.FindObjectsByType<PersistentSlowArea>(FindObjectsSortMode.None).Length==0,"Replay restores Base/resource state and clears towers and skill areas");
        check(PlayerSession.Currency.Gold==startGold+50 && PlayerSession.Currency.Candy==startCandy+5,"Replay preserves persistent currency");
    }
    public static void ClearAgain(Action<bool,string> check) { FillSchedule(check); KillAll(); }
    public static void FailRun(Action<bool,string> check) {
        StopSchedule(); FillSchedule(check);
        var enemies=Find<EnemyRegistry>().Enemies.ToArray(); enemies[0].ReceiveDamage(10000);
        failureCandy=PlayerSession.Currency.Candy;
        // Last arrival depletes Base exactly when the final enemy unregisters.
        Find<BaseHealth>().Initialize(4);
        foreach(var enemy in enemies.Skip(1)) enemy.GetComponent<EnemyPathFollower>().Advance(10000);
        check(Find<EnemyRegistry>().AliveEnemyCount==0,"Final lethal Base arrival removes the last combat enemy");
    }
    public static void VerifyFailure(Action<bool,string> check) {
        check(Find<GameFlow>().State==GameplayState.GameOver && !Find<StageVictoryController>().EvaluateClear() && PlayerSession.Currency.Gold==startGold+100,"Final lethal arrival results only in Game Over with no clear Gold");
        check(failureCandy==startCandy+11 && PlayerSession.Currency.Candy==failureCandy && Reload(PlayerSession.Currency.SavePath).Candy==failureCandy,"Candy from kill survives failure; Base arrivals award no Candy");
    }
    public static void VerifyFailureRetry(Action<bool,string> check) {
        StopSchedule();
        check(Find<GameFlow>().IsPlaying && Find<FixedIntervalSpawner>().SpawnedCount==0 && Find<EnemyRegistry>().AliveEnemyCount==0 && PlayerSession.Currency.Candy==failureCandy && PlayerSession.Currency.Gold==startGold+100 && PlayerSession.Deck.Slots.SequenceEqual(deck),"Game Over Retry resets finite run and preserves both currencies and deck");
    }
    public static void VerifyLobby(Action<bool,string> check) {
        check(Find<LobbyUI>()!=null && UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text=="Gold: "+PlayerSession.Currency.Gold+"   Candy: "+failureCandy),"Lobby reads authoritative persistent currency after clear, replay and failure");
        PlayerSession.Progression.Changed-=OnProgression;
    }
    public static void WriteRestartFixture() {
        try { PlayerSession.Ensure(); var c=PlayerSession.Currency; c.AddGold(50);c.AddCandy(7);c.TrySpendGold(10);c.TrySpendCandy(2);
            if(c.Gold!=40 || c.Candy!=5 || !c.LastSaveSucceeded) throw new Exception("Restart write mismatch");
            Debug.Log("CURRENCY RESTART WRITE PASS: Gold=40 Candy=5"); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
    public static void ReadRestartFixture() {
        try { PlayerSession.Ensure(); var c=PlayerSession.Currency;
            if(c.Gold!=40 || c.Candy!=5) throw new Exception("Restart restore mismatch");
            Debug.Log("CURRENCY RESTART READ PASS: Gold=40 Candy=5"); EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
}
}
