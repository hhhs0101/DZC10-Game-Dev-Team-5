using System;
using UnityEngine;
namespace Defense {
public sealed class GameplayHand : MonoBehaviour {
    private TurretPlacementController placement;
    private GameFlow flow;
    public RuntimeCardCycle Cycle { get; private set; }
    public RuntimeCard SelectedCard { get; private set; }
    public event Action Changed;
    public void Initialize(PlayerDeck deck, TurretPlacementController controller, GameFlow gameFlow) {
        if (placement != null) placement.Placed -= OnPlaced;
        placement = controller; flow = gameFlow;
        Cycle = new RuntimeCardCycle(deck); SelectedCard = null;
        placement.Placed += OnPlaced; placement.Select(null); Changed?.Invoke();
    }
    public bool Select(int handIndex) {
        if (!flow.IsPlaying || handIndex < 0 || handIndex >= Cycle.Hand.Count) return false;
        SelectedCard = Cycle.Hand[handIndex]; placement.Select(SelectedCard.Definition); Changed?.Invoke(); return true;
    }
    public void CancelSelection() { SelectedCard = null; placement.Select(null); Changed?.Invoke(); }
    public bool CanPlace(TurretDefinition definition) => SelectedCard != null && SelectedCard.Definition == definition && Cycle.Contains(SelectedCard);
    private void OnPlaced(TurretDefinition definition) {
        if (!CanPlace(definition)) return;
        RuntimeCard used = SelectedCard;
        SelectedCard = null; placement.Select(null);
        Cycle.Use(used); Changed?.Invoke();
    }
    private void OnDestroy() { if (placement != null) placement.Placed -= OnPlaced; }
}
}
