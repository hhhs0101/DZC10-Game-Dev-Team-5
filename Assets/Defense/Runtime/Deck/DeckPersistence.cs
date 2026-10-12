using System;
using System.IO;
using UnityEngine;
namespace Defense {
// File I/O only: domain validation and preset ownership belong to DeckPresetManager.
public sealed class DeckPersistence {
    public const string FileName = "deck-presets.json";
    public string SavePath => store.SavePath;
    private readonly JsonFileStore<DeckSaveData> store;
    public static string DefaultPath {
        get {
#if UNITY_EDITOR
            // Editor validation runs must never edit the player's real save.
            var isolated = Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
            if (!string.IsNullOrWhiteSpace(isolated)) return isolated;
#endif
            return Path.Combine(Application.persistentDataPath,FileName);
        }
    }
    public DeckPersistence(string path,Action<string> logWarning = null,Action<string> logError = null) {
        store = new JsonFileStore<DeckSaveData>(path,"Deck",logWarning,logError);
    }
    public DeckSaveData Load() => store.Load();
    public bool Save(DeckSaveData data) => store.Save(data);
}
}
