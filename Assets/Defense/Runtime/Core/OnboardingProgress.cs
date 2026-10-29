using System;
using System.IO;
using UnityEngine;
namespace Defense {
[Serializable]
public sealed class OnboardingSaveData {
    public bool storySeen;
    public bool tutorialSeen;
}
// Editable player history, independent of opening-video playback and campaign progress.
public static class OnboardingProgress {
    private static OnboardingSaveData data;
    private static JsonFileStore<OnboardingSaveData> store;
    public static string SavePath {
        get {
#if UNITY_EDITOR
            string isolated = Environment.GetEnvironmentVariable("DEFENSE_TEST_ONBOARDING_SAVE_PATH");
            if(!string.IsNullOrWhiteSpace(isolated))return Path.GetFullPath(isolated);
            foreach(string key in new[]{"DEFENSE_TEST_PROGRESS_SAVE_PATH","DEFENSE_TEST_CURRENCY_SAVE_PATH","DEFENSE_TEST_DECK_SAVE_PATH"}) {
                string other=Environment.GetEnvironmentVariable(key);
                if(!string.IsNullOrWhiteSpace(other))return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(other)),"onboarding-progress.json");
            }
#endif
            return PlayerSavePaths.FilePath("onboarding-progress.json");
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){data=null;store=null;}
    private static void EnsureLoaded() {
        if(data!=null)return;
        store=new JsonFileStore<OnboardingSaveData>(SavePath,"Story and tutorial");
        bool exists=File.Exists(SavePath);
        data=store.Load();
        if(data!=null)return;
        data=new OnboardingSaveData();
        // Import the old records only when no JSON exists. Explicit false values always win.
        if(!exists) {
            data.storySeen=PlayerPrefs.GetInt("Defense.StoryIntro.Seen.v1",0)==1;
            data.tutorialSeen=PlayerPrefs.GetInt("Defense.Tutorial.Seen.v1",0)==1;
            store.Save(data);
        }
    }
    public static bool StorySeen { get { EnsureLoaded();return data.storySeen; } }
    public static bool TutorialSeen { get { EnsureLoaded();return data.tutorialSeen; } }
    public static void MarkStorySeen(){EnsureLoaded();data.storySeen=true;store.Save(data);}
    public static void MarkTutorialSeen(){EnsureLoaded();data.tutorialSeen=true;store.Save(data);}
}
}
