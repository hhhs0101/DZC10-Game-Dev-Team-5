using System;
using UnityEngine;
namespace Defense {
public enum CardUseResult { Success, Failure, Cancelled }
public sealed class GameplayHand : MonoBehaviour {
    public const string InsufficientElixirMessage = "에너지가 부족합니다";
    private TurretPlacementController placement;
    private GameFlow flow;
    private ElixirSystem elixir;
    private StageDefinition stage;
    public RuntimeCardCycle Cycle { get; private set; }
    public RuntimeCard SelectedCard { get; private set; }
    public event Action Changed;
    public event Action<string> Message;
    public void Initialize(PlayerDeck deck, TurretPlacementController controller, GameFlow gameFlow, StageDefinition stageDefinition = null) {
        stage = stageDefinition;
        foreach(var card in deck.Slots) if(!StageCardRules.Allows(stage,card)) throw new InvalidOperationException("Deck contains a card unavailable in this stage.");
        placement = controller; flow = gameFlow; elixir = GetComponent<ElixirSystem>();
        Cycle = new RuntimeCardCycle(deck); SelectedCard = null;
        placement.Select(null); Changed?.Invoke();
    }
    // UI clicks toggle by runtime identity. Explicit programmatic Select remains idempotent.
    public bool ToggleSelection(int handIndex) {
        if (!flow.IsPlaying || handIndex < 0 || handIndex >= Cycle.Hand.Count) return false;
        if (SelectedCard == Cycle.Hand[handIndex]) { CancelSelection(); return true; }
        return Select(handIndex);
    }
    public bool Select(int handIndex) {
        if (!flow.IsPlaying || handIndex < 0 || handIndex >= Cycle.Hand.Count) return false;
        SelectedCard = Cycle.Hand[handIndex];
        placement.Select((SelectedCard.Definition as TowerCardDefinition)?.Tower); Changed?.Invoke(); return true;
    }
    public void CancelSelection() { SelectedCard = null; placement.Select(null); Changed?.Invoke(); }
    public bool CanPlace(TurretDefinition definition) => SelectedCard?.Definition is TowerCardDefinition tower && tower.Tower == definition && Cycle.Contains(SelectedCard);
    public bool CanUse(RuntimeCard card) {
        if (!flow.IsPlaying || card == null || card != SelectedCard || !Cycle.Contains(card) || !StageCardRules.Allows(stage,card.Definition)) return false;
        if (elixir.CanAfford(card.Definition.ElixirCost)) return true;
        Message?.Invoke(InsufficientElixirMessage); return false;
    }
    // Called synchronously after world validation/commit; failures and cancellations never spend or rotate.
    public CardUseResult CompleteUse(RuntimeCard card, CardUseResult result) {
        if (result != CardUseResult.Success) return result;
        if (!CanUse(card) || !elixir.TrySpend(card.Definition.ElixirCost)) return CardUseResult.Failure;
        SelectedCard = null; placement.Select(null); Cycle.Use(card); Changed?.Invoke();
        return CardUseResult.Success;
    }
}
}
