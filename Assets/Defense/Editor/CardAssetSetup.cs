using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
public static class CardAssetSetup {
    // Idempotent: preserve existing authored values and deck choices.
    [MenuItem("Defense/Create Missing Card Assets")]
    public static void Run() {
        var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Defense/Resources/GameCatalog.asset");
        if (catalog == null) return;
        var cards = new System.Collections.Generic.List<CardDefinition>();
        for (int i=0;i<catalog.AvailableTowers.Count;i++) {
            var tower = catalog.AvailableTowers[i];
            var card = Get<TowerCardDefinition>("TowerCard"+(i+1),out bool created);
            if (created) {
                var so = new SerializedObject(card); Common(so,"tower.test."+(i+1),tower.DisplayName,0);
                so.FindProperty("tower").objectReferenceValue = tower; so.ApplyModifiedPropertiesWithoutUndo();
            }
            cards.Add(card);
        }
        var effect = Get<CircularDamageSkill>("CircularDamageSkill",out _);
        for (int i=0;i<2;i++) {
            string name = i==0 ? "Fireball" : "ArrowRain";
            var skill = Get<SkillDefinition>(name+"Skill",out bool created);
            if (created) {
                var so = new SerializedObject(skill);
                so.FindProperty("damage").floatValue = i==0 ? 60 : 25;
                so.FindProperty("radius").floatValue = i==0 ? 1.5f : 3;
                so.FindProperty("effect").objectReferenceValue = effect; so.ApplyModifiedPropertiesWithoutUndo();
            }
            var card = Get<SkillCardDefinition>(name+"Card",out created);
            if (created) {
                var so = new SerializedObject(card); Common(so,"skill."+name.ToLowerInvariant(),i==0 ? "Fireball" : "Arrow Rain",i==0 ? 3 : 2);
                so.FindProperty("skill").objectReferenceValue = skill; so.ApplyModifiedPropertiesWithoutUndo();
            }
            cards.Add(card);
        }
        foreach (var existing in catalog.AvailableCards) if (existing != null && !cards.Contains(existing)) cards.Add(existing);
        var data = new SerializedObject(catalog);
        var all = data.FindProperty("availableCards"); all.arraySize = cards.Count;
        for (int i=0;i<cards.Count;i++) all.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        var deck = data.FindProperty("initialDeck");
        if (deck.arraySize != 6 || Enumerable.Range(0,deck.arraySize).Any(i=>deck.GetArrayElementAtIndex(i).objectReferenceValue == null)) {
            deck.arraySize = 6; for (int i=0;i<6;i++) deck.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets();
    }
    private static T Get<T>(string name, out bool created) where T : ScriptableObject {
        string path = "Assets/Defense/Data/"+name+".asset";
        var value = AssetDatabase.LoadAssetAtPath<T>(path); created = value == null;
        if (created) { value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value,path); }
        return value;
    }
    private static void Common(SerializedObject so,string id,string name,float cost) {
        so.FindProperty("cardId").stringValue = id; so.FindProperty("displayName").stringValue = name; so.FindProperty("elixirCost").floatValue = cost;
    }
}
}
