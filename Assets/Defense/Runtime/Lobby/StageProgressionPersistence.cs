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
    private readonly Action<string> warning, error;
    private readonly string legacyPath;
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
            return PlayerSavePaths.FilePath(FileName);
        }
    }
    public StageProgressionPersistence(string path,Action<string> logWarning = null,Action<string> logError = null,string legacySavePath = null) {
        warning = logWarning ?? (message=>Debug.LogWarning(message));
        error = logError ?? (message=>Debug.LogError(message));
        store = new JsonFileStore<StageProgressionSaveData>(path,"Stage progression",warning,error);
        // Custom/isolated paths never import a real player's external save by accident.
        legacyPath = legacySavePath ?? (SavePath==PlayerSavePaths.FilePath(FileName)
            ? Path.Combine(Application.persistentDataPath,FileName) : null);
    }
    public StageProgressionSaveData Load() {
        bool migrate=!File.Exists(SavePath)&&!string.IsNullOrWhiteSpace(legacyPath);
        var data = migrate
            ? new JsonFileStore<StageProgressionSaveData>(legacyPath,"Legacy stage progression",warning,error).Load()
            : store.Load();
        if (data != null && data.Version != SaveVersion) {
            warning("Unsupported stage progression version; using defaults without overwriting this save: "+SavePath);
            CanWrite = false; return null;
        }
        if(migrate&&data!=null)store.Save(data); // Keep the original as a migration backup.
        return data;
    }
    public bool Save(StageProgressionSaveData data) => CanWrite && store.Save(data);
}
}
