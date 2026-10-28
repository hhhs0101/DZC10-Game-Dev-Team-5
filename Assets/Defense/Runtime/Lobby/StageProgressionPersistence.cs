using System;
using System.IO;
using UnityEngine;
namespace Defense {
[Serializable]
public sealed class StageProgressionSaveData {
    public int Version;
    public string[] CompletedStageIds;
    public string[] UnlockedStageIds;
}
// Uses the same atomic JSON file storage as decks and currency, in a separate file.
public sealed class StageProgressionPersistence {
    public const string FileName = "stage-progress.json";
    public const int SaveVersion = 1;
    private readonly JsonFileStore<StageProgressionSaveData> store;
    private readonly Action<string> warning;
    public string SavePath => store.SavePath;
    public bool CanWrite { get; private set; } = true;
    public static string DefaultPath {
        get {
#if UNITY_EDITOR
            var isolated = Environment.GetEnvironmentVariable("DEFENSE_TEST_PROGRESS_SAVE_PATH");
            if (!string.IsNullOrWhiteSpace(isolated)) return isolated;
            // Existing validators already isolate currency/deck; never leak their clears to real progress.
            var other = Environment.GetEnvironmentVariable("DEFENSE_TEST_CURRENCY_SAVE_PATH");
            if (string.IsNullOrWhiteSpace(other)) other = Environment.GetEnvironmentVariable("DEFENSE_TEST_DECK_SAVE_PATH");
            if (!string.IsNullOrWhiteSpace(other)) return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(other)),FileName);
#endif
            return Path.Combine(Application.persistentDataPath,FileName);
        }
    }
    public StageProgressionPersistence(string path,Action<string> logWarning = null,Action<string> logError = null) {
        warning = logWarning ?? (message=>Debug.LogWarning(message));
        store = new JsonFileStore<StageProgressionSaveData>(path,"Stage progression",warning,logError);
    }
    public StageProgressionSaveData Load() {
        var data = store.Load();
        if (data != null && data.Version != SaveVersion) {
            warning("Unsupported stage progression version; using defaults without overwriting this save: "+SavePath);
            CanWrite = false; return null;
        }
        return data;
    }
    public bool Save(StageProgressionSaveData data) => CanWrite && store.Save(data);
}
}
