using System;
using UnityEngine;
namespace Defense {
public enum CardUseResult { Success, Failure, Cancelled }
public sealed class GameplayHand : MonoBehaviour {
    public const string InsufficientElixirMessage = "엘릭서가 부족합니다";
    private TurretPlacementController placement;
    private GameFlow flow;
    private ElixirSystem elixir;
    public RuntimeCardCycle Cycle { get; private set; }
    public RuntimeCard SelectedCard { get; private set; }
    public event Action Changed;
    public event Action<string> Message;
    public void Initialize(PlayerDeck deck, TurretPlacementController controller, GameFlow gameFlow) {
        placement = controller; flow = gameFlow; elixir = GetComponent<ElixirSystem>();
        Cycle = new RuntimeCardCycle(deck); SelectedCard = null;
        placement.Select(null); Changed?.Invoke();
    }
    public bool Select(int handIndex) {
        if (!flow.IsPlaying || handIndex < 0 || handIndex >= Cycle.Hand.Count) return false;
        SelectedCard = Cycle.Hand[handIndex];
        placement.Select((SelectedCard.Definition as TowerCardDefinition)?.Tower); Changed?.Invoke(); return true;
    }
    public void CancelSelection() { SelectedCard = null; placement.Select(null); Changed?.Invoke(); }
    public bool CanPlace(TurretDefinition definition) => SelectedCard?.Definition is TowerCardDefinition tower && tower.Tower == definition && Cycle.Contains(SelectedCard);
    public bool CanUse(RuntimeCard card) {
        if (!flow.IsPlaying || card == null || card != SelectedCard || !Cycle.Contains(card)) return false;
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
