using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
public static class StageCardProgressionValidation {
    [MenuItem("Defense/Validate Stage Card Progression")]
    public static void Run() {
        var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Defense/Resources/GameCatalog.asset");
        var stages=catalog.Stages;
        Check(stages.Count==3,"Three campaign stages");
        var counts=new[]{3,6,9};var prior=new HashSet<CardDefinition>();
        string directory=Path.Combine(Path.GetTempPath(),"defense-stage-cards-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string legacy=Path.Combine(directory,"deck-presets.json");File.WriteAllText(legacy,"Legacy save must remain untouched");
        var managers=new List<DeckPresetManager>();
        try {
            var paths=new HashSet<string>();
            for(int i=0;i<stages.Count;i++) {
                var stage=stages[i];StageCardRules.Validate(stage,catalog);
                var allowed=new HashSet<CardDefinition>(stage.AllowedCards);
                Check(allowed.Count==counts[i]&&prior.IsSubsetOf(allowed),"Cumulative stage pool "+i);
                Check(stage.DefaultDeck.Count==Math.Min(6,counts[i])&&stage.DefaultDeck.Distinct().Count()==stage.DefaultDeck.Count&&stage.DefaultDeck.All(allowed.Contains),"Valid stage default deck");
                if(i==0)Check(allowed.All(c=>c is TowerCardDefinition t&&t.Tower.TowerType!=TowerType.HotSauce&&t.Tower.TowerType!=TowerType.Mayonnaise&&t.Tower.TowerType!=TowerType.Mustard&&t.Tower.TowerType!=TowerType.Ketchup&&t.Tower.TowerType!=TowerType.Oil),"Stage 1 direct-damage cards only");
                string path=StageCardRules.SavePath(stage,legacy);Check(path!=legacy&&paths.Add(path),"Separate stage save paths");
                var manager=new DeckPresetManager(stage.DefaultDeck,stage.AllowedCards,new DeckPersistence(path));managers.Add(manager);
                var editor=new DeckEditorController(manager.ActiveDeck,stage.AllowedCards);
                foreach(var locked in catalog.AvailableCards.Where(c=>!allowed.Contains(c))) {
                    Check(!editor.Replace(0,locked),"Editor rejects later-stage card");
                    Check(!StageCardRules.Allows(stage,locked),"Runtime rejects later-stage card");
                }
                prior=allowed;
            }
            for(int i=0;i<3;i++) {
                var cycle=new RuntimeCardCycle(new PlayerDeck(stages[i].DefaultDeck),new System.Random(42));
                Check(cycle.Hand.Count==3&&cycle.UpcomingQueue.Count==stages[i].DefaultDeck.Count-3,"Correct hand and queue sizes");
                for(int turn=0;turn<24;turn++) {
                    Check(cycle.Use(cycle.Hand[0]),"Card cycles successfully");
                    Check(cycle.Hand.Concat(cycle.UpcomingQueue).Select(c=>c.Definition).Distinct().Count()==stages[i].DefaultDeck.Count,"No missing or duplicate cards after cycling");
                    Check(cycle.Hand.All(c=>StageCardRules.Allows(stages[i],c.Definition)),"Hand remains restricted");
                }
            }
            using(var restored=new DeckPresetManager(stages[0].DefaultDeck,stages[0].AllowedCards,new DeckPersistence(StageCardRules.SavePath(stages[0],legacy))))
                Check(restored.ActiveDeck.Slots.Count==3,"Three-card preset survives save and restore");
            var oneBefore=managers[0].ActiveDeck.Slots.ToArray();
            var secondEditor=new DeckEditorController(managers[2].ActiveDeck,stages[2].AllowedCards);
            var extra=secondEditor.Available.First();Check(secondEditor.Replace(0,extra),"Allowed card can be equipped");
            Check(managers[0].ActiveDeck.Slots.SequenceEqual(oneBefore),"Stage 3 edit does not change Stage 1");
            managers[2].SwitchPreset(1);Check(managers[2].ActiveDeck.Slots.SequenceEqual(stages[2].DefaultDeck),"Independent presets within a stage");
            managers[2].SwitchPreset(0);
            using(var restored=new DeckPresetManager(stages[2].DefaultDeck,stages[2].AllowedCards,new DeckPersistence(StageCardRules.SavePath(stages[2],legacy)))) {
                Check(restored.ActiveDeck.Slots[0]==extra,"Stage deck persists and restores");
            }
            // Put a valid Stage 3 preset in a Stage 1 file to simulate stale/foreign data.
            string restricted=StageCardRules.SavePath(stages[0],legacy);
            File.Copy(StageCardRules.SavePath(stages[2],legacy),restricted,true);
            using(var repaired=new DeckPresetManager(stages[0].DefaultDeck,stages[0].AllowedCards,new DeckPersistence(restricted),_=>{}))
                Check(repaired.ActiveDeck.Slots.All(c=>StageCardRules.Allows(stages[0],c)),"Foreign locked cards are repaired on load");
            Check(File.ReadAllText(legacy)=="Legacy save must remain untouched","Legacy global presets preserved");
            Debug.Log("STAGE CARD PROGRESSION PASSED: pools, restrictions, independent presets, save/restore and locked-save recovery.");
        } finally {
            foreach(var manager in managers)manager.Dispose();
            Directory.Delete(directory,true);
        }
    }
    private static void Check(bool condition,string message) { if(!condition)throw new Exception(message); }
}
}
