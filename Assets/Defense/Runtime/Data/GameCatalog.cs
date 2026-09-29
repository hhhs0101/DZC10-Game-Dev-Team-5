using System.Collections.Generic;
using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Game Catalog")]
public sealed class GameCatalog : ScriptableObject {
    [SerializeField] private StageDefinition[] stages;
    [SerializeField] private TurretDefinition[] availableTowers;
    [SerializeField, Tooltip("Exactly six unique card definitions in persistent slots T1 through T6.")] private CardDefinition[] initialDeck;
    [SerializeField] private CardDefinition[] availableCards;
    public IReadOnlyList<CardDefinition> AvailableCards => System.Array.AsReadOnly(availableCards ?? System.Array.Empty<CardDefinition>());
    public IReadOnlyList<StageDefinition> Stages => System.Array.AsReadOnly(stages);
    public IReadOnlyList<TurretDefinition> AvailableTowers => System.Array.AsReadOnly(availableTowers);
    public IReadOnlyList<CardDefinition> InitialDeck => System.Array.AsReadOnly(initialDeck);
}
}
