using UnityEngine;
namespace Defense {
// Session-only ownership. No scene objects, combat instances, or disk saves are retained.
public static class PlayerSession {
    public static GameCatalog Catalog { get; private set; }
    public static PlayerDeck Deck { get; private set; }
    public static StageProgressionState Progression { get; private set; }
    public static StageSelection Selection { get; private set; }
    public static StageDefinition ActiveStage { get; private set; }
    public static bool ReturnToLobby { get; set; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { Catalog = null; Deck = null; Progression = null; Selection = null; ActiveStage = null; ReturnToLobby = false; }
    public static void Ensure() {
        if (Catalog != null) return;
        Catalog = Resources.Load<GameCatalog>("GameCatalog");
        if (Catalog == null) throw new System.InvalidOperationException("Resources/GameCatalog asset is required.");
        Deck = new PlayerDeck(Catalog.InitialDeck);
        Progression = new StageProgressionState(Catalog.Stages.Count);
        Selection = new StageSelection(Catalog.Stages.Count);
    }
    public static bool TrySelectStageForPlay() {
        Ensure(); int index = Selection.SelectedStageIndex;
        if (!Progression.IsUnlocked(index)) return false;
        ActiveStage = Catalog.Stages[index]; ReturnToLobby = true; return true;
    }
    // Integration point for a future victory objective; Game Over must never call it.
    public static bool CompleteActiveStage() {
        Ensure();
        for (int i=0;i<Catalog.Stages.Count;i++) if (Catalog.Stages[i] == ActiveStage) return Progression.Complete(i);
        return false;
    }
}
}
