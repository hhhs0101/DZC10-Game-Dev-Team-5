using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Defense.Editor {
public static class DeckPersistenceValidation {
    public const string TestPathVariable = "DEFENSE_TEST_DECK_SAVE_PATH";
    private static string lastBattleSave;
    public static void Models(GameCatalog catalog,Action<bool,string> check) {
        string folder=Path.Combine(Path.GetTempPath(),"defense-deck-model-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var warnings=new List<string>(); var errors=new List<string>();
        var storage=new DeckPersistence(Path.Combine(folder,"deck.json"),warnings.Add,errors.Add);
        var cards=catalog.AvailableCards; var defaults=catalog.InitialDeck;
        Func<DeckPresetManager> load=()=>new DeckPresetManager(defaults,cards,storage,warnings.Add);
        try {
            using(var presets=load()) {
                check(File.Exists(storage.SavePath) && presets.LastSaveSucceeded && presets.ActivePresetIndex==0,"First launch saves three valid default presets and selects Preset 1");
                check(Enumerable.Range(0,3).All(i=>presets.GetPreset(i).Slots.SequenceEqual(defaults)) && !ReferenceEquals(presets.GetPreset(0),presets.GetPreset(1)) && !ReferenceEquals(presets.GetPreset(1),presets.GetPreset(2)),"Default presets own independent existing PlayerDeck models");
                var editor=new DeckEditorController(presets.ActiveDeck,cards);
                check(editor.Replace(0,cards[6]) && storage.Load().Presets[0].CardIds[0]==cards[6].CardId,"Valid edits autosave immediately using stable Card IDs");
                check(presets.GetPreset(1).Slots.SequenceEqual(defaults) && presets.GetPreset(2).Slots.SequenceEqual(defaults),"Editing one preset leaves both others unchanged");
                string before=File.ReadAllText(storage.SavePath);
                check(!editor.Replace(1,cards[6]) && !editor.Replace(-1,cards[7]) && File.ReadAllText(storage.SavePath)==before,"Duplicate/invalid edits leave saved bytes unchanged");
                check(!presets.SwitchPreset(-1) && !presets.SwitchPreset(3) && File.ReadAllText(storage.SavePath)==before,"Invalid preset selection is rejected without saving");
                presets.SwitchPreset(1); new DeckEditorController(presets.ActiveDeck,cards).Replace(1,cards[7]);
                presets.SwitchPreset(2); new DeckEditorController(presets.ActiveDeck,cards).Replace(2,cards[8]);
                check(storage.Load().ActivePresetIndex==2 && storage.Load().Version==1 && storage.Load().Presets.Length==3,"Active preset and version-one three-preset schema persist");
                before=File.ReadAllText(storage.SavePath);
                var snapshot=presets.ActiveDeck.Slots.ToArray(); var runtime=new RuntimeCardCycle(presets.ActiveDeck,new System.Random(19));
                for(int i=0;i<30;i++) runtime.Use(runtime.Hand[i%3]);
                check(File.ReadAllText(storage.SavePath)==before && presets.ActiveDeck.Slots.SequenceEqual(snapshot),"Runtime shuffle and cycling never write back into persistent presets");
            }
            using(var restored=new DeckPresetManager(defaults,cards.Reverse().ToArray(),storage,warnings.Add)) {
                check(restored.ActivePresetIndex==2 && restored.GetPreset(0).Slots[0]==cards[6] && restored.GetPreset(1).Slots[1]==cards[7] && restored.GetPreset(2).Slots[2]==cards[8],"Fresh manager restores all independent presets and selection despite catalog reordering");
                check(new DeckEditorController(restored.ActiveDeck,cards).Available.All(c=>!restored.ActiveDeck.Contains(c)),"Restored active deck drives Available Cards exclusion");
            }
            string good=File.ReadAllText(storage.SavePath);
            foreach(var invalid in new[]{"missing","duplicate","short","null","empty-id"}) {
                File.WriteAllText(storage.SavePath,good); var data=storage.Load();
                if(invalid=="missing") data.Presets[1].CardIds[0]="deleted.card";
                if(invalid=="duplicate") data.Presets[1].CardIds[0]=data.Presets[1].CardIds[1];
                if(invalid=="short") data.Presets[1].CardIds=new[]{cards[0].CardId};
                if(invalid=="null") data.Presets[1]=null;
                if(invalid=="empty-id") data.Presets[1].CardIds[0]="";
                storage.Save(data); warnings.Clear();
                using(var repaired=load()) {
                    check(repaired.GetPreset(0).Slots[0]==cards[6] && repaired.GetPreset(1).Slots.SequenceEqual(defaults) && repaired.GetPreset(2).Slots[2]==cards[8] && repaired.ActivePresetIndex==2,"Only invalid preset is repaired: "+invalid);
                    check(warnings.Count>0 && storage.Load().Presets[1].CardIds.SequenceEqual(defaults.Select(c=>c.CardId)),"Repaired preset is logged and saved: "+invalid);
                    if(invalid=="missing") check(warnings.Any(w=>w.Contains("deleted.card")),"Recovery warning identifies the missing Card ID");
                }
            }
            foreach(string json in new[]{"not json","null","{}"}) {
                File.WriteAllText(storage.SavePath,json);
                using(var recovered=load()) check(recovered.ActivePresetIndex==0 && Enumerable.Range(0,3).All(i=>recovered.GetPreset(i).Slots.SequenceEqual(defaults)) && storage.Load().Version==1,"Malformed/empty save recovers safely and persists defaults: "+json);
            }
            File.WriteAllText(storage.SavePath,good); var badIndex=storage.Load(); badIndex.ActivePresetIndex=99; storage.Save(badIndex);
            using(var repaired=load()) check(repaired.ActivePresetIndex==0 && repaired.GetPreset(2).Slots[2]==cards[8],"Invalid active index repairs selection without losing valid presets");
            File.WriteAllText(storage.SavePath,good); var shortArray=storage.Load(); shortArray.Presets=shortArray.Presets.Take(2).ToArray(); storage.Save(shortArray);
            using(var repaired=load()) check(repaired.GetPreset(0).Slots[0]==cards[6] && repaired.GetPreset(1).Slots[1]==cards[7] && repaired.GetPreset(2).Slots.SequenceEqual(defaults),"Missing preset record preserves all valid siblings");
            var future=storage.Load(); future.Version=99; storage.Save(future);
            using(var repaired=load()) check(repaired.GetPreset(0).Slots.SequenceEqual(defaults) && storage.Load().Version==1,"Unsupported version restores a valid version-one save");
            // A failed atomic replacement cannot corrupt the old save or roll back an accepted in-memory edit.
            using(var presets=load()) {
                string before=File.ReadAllText(storage.SavePath); Directory.CreateDirectory(storage.SavePath+".tmp");
                check(new DeckEditorController(presets.ActiveDeck,cards).Replace(0,cards[6]) && !presets.LastSaveSucceeded && presets.ActiveDeck.Slots[0]==cards[6] && File.ReadAllText(storage.SavePath)==before && errors.Any(e=>e.Contains("NOT saved")),"Write failure preserves memory and old file while explicitly reporting persistence failure");
                Directory.Delete(storage.SavePath+".tmp");
                check(new DeckEditorController(presets.ActiveDeck,cards).Replace(1,cards[7]) && presets.LastSaveSucceeded && storage.Load().Presets[0].CardIds[0]==cards[6].CardId,"Later successful autosave persists the full current state after a write failure");
            }
            var skills=Enumerable.Range(0,6).Select(i=> {
                var clone=UnityEngine.Object.Instantiate(cards.OfType<SkillCardDefinition>().First());
                var serialized=new SerializedObject(clone); serialized.FindProperty("cardId").stringValue="test.skill."+i; serialized.ApplyModifiedPropertiesWithoutUndo(); return clone;
            }).ToArray();
            try {
                var expanded=cards.Concat(skills).ToArray(); var data=storage.Load(); data.Presets[1].CardIds=skills.Select(c=>c.CardId).ToArray(); storage.Save(data);
                using(var repaired=new DeckPresetManager(defaults,expanded,storage,warnings.Add)) check(repaired.GetPreset(1).Slots.SequenceEqual(defaults),"Persistence validation rejects six unique skill cards without a Tower");
                data=storage.Load(); data.Presets[1].CardIds=new[]{cards[0].CardId}.Concat(skills.Take(5).Select(c=>c.CardId)).ToArray(); data.ActivePresetIndex=1; storage.Save(data);
                using(var presets=new DeckPresetManager(defaults,expanded,storage,warnings.Add)) {
                    string before=File.ReadAllText(storage.SavePath); string message=null; presets.ActiveDeck.Rejected+=value=>message=value;
                    check(!new DeckEditorController(presets.ActiveDeck,expanded).Replace(0,skills[5]) && message==PlayerDeck.MinimumTowerMessage && File.ReadAllText(storage.SavePath)==before,"Final-Tower protection rejects edit and preserves saved bytes in an active preset");
                }
            } finally { foreach(var card in skills) UnityEngine.Object.DestroyImmediate(card); }
        } finally { Directory.Delete(folder,true); }
    }
    public static void EditorUI(Action<bool,string> check) {
        var presets=PlayerSession.Presets; var catalog=PlayerSession.Catalog;
        var ui=UnityEngine.Object.FindFirstObjectByType<DeckEditorUI>();
        var buttons=ui.GetComponentsInChildren<DeckPresetButtons>().Single().GetComponentsInChildren<Button>();
        check(buttons.Length==3 && buttons.Select(b=>b.GetComponentInChildren<Text>().text).SequenceEqual(new[]{"Preset 1","Preset 2","Preset 3"}),"Deck Editor provides exactly the three requested preset buttons");
        var original=presets.ActiveDeck.Slots.ToArray();
        var source=UnityEngine.Object.FindObjectsByType<DeckCardDrag>().First(c=>c.Definition==catalog.AvailableCards[13]);
        var pointer=new PointerEventData(EventSystem.current) { pointerDrag=source.gameObject,button=PointerEventData.InputButton.Left,position=new Vector2(200,200) };
        ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.beginDragHandler);
        buttons[1].onClick.Invoke();
        var slots=ui.GetComponentsInChildren<DeckSlotDrop>().OrderBy(s=>s.name).ToArray();
        check(presets.ActivePresetIndex==1 && PlayerSession.Deck==presets.GetPreset(1) && !source.IsDragging,"Preset click immediately changes sole active deck and cancels an in-progress drag");
        ExecuteEvents.Execute(slots[0].gameObject,pointer,ExecuteEvents.dropHandler);
        check(presets.ActiveDeck.Slots.SequenceEqual(catalog.InitialDeck),"Stale drag from the previous preset cannot edit the new preset");
        check(!buttons[1].interactable && buttons[0].interactable && buttons[1].image.color!=buttons[0].image.color,"Active preset has a distinct selected visual state");
        check(slots.Select((s,i)=>s.GetComponentInChildren<Text>().text.Contains(presets.ActiveDeck.Slots[i].DisplayName)).All(v=>v),"All six slot labels refresh immediately on preset switch");
        ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.beginDragHandler); ExecuteEvents.Execute(slots[0].gameObject,pointer,ExecuteEvents.dropHandler); ExecuteEvents.Execute(source.gameObject,pointer,ExecuteEvents.endDragHandler);
        check(presets.GetPreset(1).Slots[0]==source.Definition && presets.GetPreset(0).Slots.SequenceEqual(original),"Drop targets rebind to selected preset while preserving the previous preset");
        var available=UnityEngine.Object.FindObjectsByType<DeckCardDrag>();
        check(available.Length==11 && available.All(c=>!presets.ActiveDeck.Contains(c.Definition)),"Available UI excludes exactly the current preset after switch and edit");
        buttons[2].onClick.Invoke();
        var distinct= catalog.AvailableCards[12]; new DeckEditorController(presets.ActiveDeck,catalog.AvailableCards).Replace(1,distinct);
        buttons[0].onClick.Invoke();
        check(presets.ActiveDeck.Slots.SequenceEqual(original) && presets.GetPreset(1).Slots[0]==source.Definition && presets.GetPreset(2).Slots[1]==distinct,"UI switching keeps all three independently edited compositions");
        check(new DeckPersistence(presets.SavePath).Load().ActivePresetIndex==0,"Preset button persists its active index immediately");
        buttons[2].onClick.Invoke(); // Choose the stage deck inside the Deck Editor, not the Lobby.
    }
    public static void LobbyUI(Action<bool,string> check) {
        var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyUI>();
        check(lobby.GetComponentsInChildren<DeckPresetButtons>(true).Length==0 && !lobby.GetComponentsInChildren<Button>(true).Any(b=>b.GetComponentInChildren<Text>().text.StartsWith("Preset ")),"Main Lobby has no preset controls");
        check(PlayerSession.Presets.ActivePresetIndex==2 && GameObject.Find("Deck Preview T2").GetComponentInChildren<Text>().text.Contains(PlayerSession.Deck.Slots[1].DisplayName),"Lobby preview preserves the preset selected in the Deck Editor");
        // Leave a non-default preset selected for the existing stage/retry regression.
        lastBattleSave=File.ReadAllText(PlayerSession.Presets.SavePath);
    }
    public static void VerifyBattle(Action<bool,string> check,bool retry) {
        var cycle=UnityEngine.Object.FindFirstObjectByType<GameplayHand>().Cycle;
        check(cycle.InitialOrder.Select(c=>c.Definition).SequenceEqual(cycle.InitialOrder.Select(c=>PlayerSession.Deck.Slots[c.SourceSlot])),"Stage/Retry runtime copy uses the active preset's persistent slots");
        if(!retry) check(PlayerSession.Presets.ActivePresetIndex==2,"Stage starts from selected non-default Preset 3");
        check(File.ReadAllText(PlayerSession.Presets.SavePath)==lastBattleSave,"Stage shuffle, card use and Retry do not change saved preset bytes");
    }
    public static void SnapshotBeforeRetry() => lastBattleSave=File.ReadAllText(PlayerSession.Presets.SavePath);
    // These entry points run in TWO separate Unity processes against one isolated save path.
    public static void WriteRestartFixture() => Restart(false);
    public static void ReadRestartFixture() => Restart(true);
    private static void Restart(bool read) {
        try {
            string isolated=Environment.GetEnvironmentVariable(TestPathVariable);
            if(string.IsNullOrWhiteSpace(isolated)) throw new Exception("Restart validation requires an isolated save path.");
            PlayerSession.Ensure(); var presets=PlayerSession.Presets; var catalog=PlayerSession.Catalog;
            if(!read) {
                for(int i=0;i<3;i++) { presets.SwitchPreset(i); if(!new DeckEditorController(PlayerSession.Deck,catalog.AvailableCards).Replace(i,catalog.AvailableCards[6+i])) throw new Exception("Fixture edit failed."); }
                presets.SwitchPreset(1);
                if(!presets.LastSaveSucceeded) throw new Exception("Fixture save failed.");
            } else {
                if(presets.ActivePresetIndex!=1) throw new Exception("Active preset lost after process restart.");
                for(int i=0;i<3;i++) if(presets.GetPreset(i).Slots[i]!=catalog.AvailableCards[6+i]) throw new Exception("Preset "+i+" lost after process restart.");
                var cycle=new RuntimeCardCycle(PlayerSession.Deck);
                if(!new HashSet<CardDefinition>(cycle.InitialOrder.Select(c=>c.Definition)).SetEquals(presets.GetPreset(1).Slots)) throw new Exception("Restored active preset not used by cycle.");
            }
            string result=read?"DECK_PROCESS_RESTART_READ_PASSED":"DECK_PROCESS_RESTART_WRITE_PASSED";
            Debug.Log(result); Directory.CreateDirectory("Documentation/Report"); File.WriteAllText("Documentation/Report/"+result+".txt",result+"\n");
            EditorApplication.Exit(0);
        } catch(Exception exception) { Debug.LogError(exception); EditorApplication.Exit(1); }
    }
}
}
