using UnityEngine;
namespace Defense {
// Currency and deck presets persist to disk; progression and stage selection remain session-only.
public static class PlayerSession {
    public static GameCatalog Catalog { get; private set; }
    public static DeckPresetManager Presets { get; private set; }
    public static PlayerCurrency Currency { get; private set; }
    public static PlayerDeck Deck => Presets?.ActiveDeck;
    public static StageProgressionState Progression { get; private set; }
    public static StageSelection Selection { get; private set; }
    public static StageDefinition ActiveStage { get; private set; }
    // Alternate presentation keeps the original StageDefinition/catalog identity and save data.
    public const string TopDownSceneName = "TopDownStage3";
    public static string ActiveBattleScene { get; private set; }
    public static bool ReturnToLobby { get; set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Presets?.Dispose(); Presets = null; Currency = null; Catalog = null; Progression = null; Selection = null; ActiveStage = null; ActiveBattleScene = null; ReturnToLobby = false; }
    public static void Ensure() {
        if (Catalog != null) return;
        Catalog = Resources.Load<GameCatalog>("GameCatalog");
        if (Catalog == null) throw new System.InvalidOperationException("Resources/GameCatalog asset is required.");
        Presets = new DeckPresetManager(Catalog.InitialDeck,Catalog.AvailableCards,new DeckPersistence(DeckPersistence.DefaultPath));
        Currency = new PlayerCurrency(new JsonFileStore<PlayerCurrencySaveData>(PlayerCurrency.DefaultPath,"Currency"));
        Progression = new StageProgressionState(Catalog.Stages);
        Selection = new StageSelection(Catalog.Stages.Count);
    }
    public static bool TrySelectStageForPlay(bool useTopDown = true) {
        Ensure(); int index = Selection.SelectedStageIndex;
        if (!Progression.IsUnlocked(index)) return false;
        ActiveStage = Catalog.Stages[index];
        ActiveBattleScene = useTopDown ? TopDownSceneName : ActiveStage.SceneName;
        ReturnToLobby = true; return true;
    }
    // Scene entry also supports opening the configured TestStage scene directly in the Editor.
    internal static void BindActiveStage(StageDefinition definition, string sceneName = null) {
        ActiveStage = definition; ActiveBattleScene = sceneName ?? definition.SceneName;
    }
    // Idempotent progression only. Victory reward policy is deliberately separate.
    public static bool CompleteActiveStage() {
        Ensure();
        for (int i=0;i<Catalog.Stages.Count;i++) if (Catalog.Stages[i] == ActiveStage) return Progression.Complete(i);
        return false;
    }
}
}
