using UnityEngine;
namespace Defense {
// Currency and deck presets persist to disk; progression and stage selection remain session-only.
public static class PlayerSession {
    public static GameCatalog Catalog { get; private set; }
    public static DeckPresetManager Presets { get; private set; }
    public static PlayerCurrency Currency { get; private set; }
    private static readonly System.Collections.Generic.Dictionary<StageDefinition,DeckPresetManager> stagePresets = new System.Collections.Generic.Dictionary<StageDefinition,DeckPresetManager>();
    public static event System.Action StageCardsChanged;
    public static StageDefinition CardStage { get; private set; }
    public static System.Collections.Generic.IReadOnlyList<CardDefinition> AvailableCards => StageCardRules.Available(CardStage,Catalog);
    public static PlayerDeck Deck => Presets?.ActiveDeck;
    public static StageProgressionState Progression { get; private set; }
    public static StageSelection Selection { get; private set; }
    public static StageDefinition ActiveStage { get; private set; }
    // Authoring constant for the current shared 2D battlefield; runtime routing uses each stage definition.
    public const string TopDownSceneName = "TopDownStage3";
    public static string ActiveBattleScene => ActiveStage != null ? ActiveStage.SceneName : null;
    public static bool ReturnToLobby { get; set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { foreach(var manager in stagePresets.Values) manager.Dispose(); stagePresets.Clear();
        StageCardsChanged = null; CardStage = null; Presets = null; Currency = null; Catalog = null; Progression = null; Selection = null; ActiveStage = null; ReturnToLobby = false; }
    public static void Ensure() {
        if (Catalog != null) return;
        Catalog = Resources.Load<GameCatalog>("GameCatalog");
        if (Catalog == null) throw new System.InvalidOperationException("Resources/GameCatalog asset is required.");
        Currency = new PlayerCurrency(new JsonFileStore<PlayerCurrencySaveData>(PlayerCurrency.DefaultPath,"Currency"));
        Progression = new StageProgressionState(Catalog.Stages);
        Selection = new StageSelection(Catalog.Stages.Count);
        Selection.Changed += () => SelectCardStage(Catalog.Stages[Selection.SelectedStageIndex]);
        SelectCardStage(Catalog.Stages[Selection.SelectedStageIndex]);
    }
    public static void SelectCardStage(StageDefinition stage) {
        if(stage == null) return;
        StageCardRules.Validate(stage,Catalog);
        if(!stagePresets.TryGetValue(stage,out var manager)) {
            // Keep the old global save intact; each stage owns three independent presets.
            string path = StageCardRules.SavePath(stage,DeckPersistence.DefaultPath);
            manager = new DeckPresetManager(StageCardRules.Defaults(stage,Catalog),StageCardRules.Available(stage,Catalog),new DeckPersistence(path));
            stagePresets.Add(stage,manager);
        }
        if(CardStage == stage && Presets == manager) return;
        CardStage = stage; Presets = manager; StageCardsChanged?.Invoke();
    }
    public static bool TrySelectStageForPlay() => TrySelectStageForPlay(out _);
    public static bool TrySelectStageForPlay(out string issue) {
        Ensure(); int index = Selection.SelectedStageIndex;
        if (!Progression.IsUnlocked(index)) { issue = "This stage is locked."; return false; }
        var stage = Catalog.Stages[index];
        if (!TryValidateStageScene(stage,out issue)) { Debug.LogError(issue); return false; }
        ActiveStage = stage;
        ReturnToLobby = true; return true;
    }
    // Direct opening of a configured 2D scene keeps its authored stage identity.
    internal static void BindActiveStage(StageDefinition definition) {
        ActiveStage = definition;
        for(int i=0;i<Catalog.Stages.Count;i++) if(Catalog.Stages[i] == definition) { Selection.Select(i); break; }
        SelectCardStage(definition);
    }
    public static bool TryValidateStageScene(StageDefinition stage, out string issue) {
        issue = null;
        if (stage == null || string.IsNullOrWhiteSpace(stage.SceneName) || !Application.CanStreamedLevelBeLoaded(stage.SceneName)) {
            issue = "Stage '"+(stage != null ? stage.DisplayName : "<missing>")+"' requires an available 2D scene in Build Settings: '"+(stage != null ? stage.SceneName : "")+"'.";
            return false;
        }
        return true;
    }
    // Idempotent progression only. Victory reward policy is deliberately separate.
    public static bool CompleteActiveStage() {
        Ensure();
        for (int i=0;i<Catalog.Stages.Count;i++) if (Catalog.Stages[i] == ActiveStage) return Progression.Complete(i);
        return false;
    }
}
}
