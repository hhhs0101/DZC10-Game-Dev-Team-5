using System.Collections.Generic;
using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Game Catalog")]
public sealed class GameCatalog : ScriptableObject {
    [SerializeField] private StageDefinition[] stages;
    [SerializeField] private TurretDefinition[] availableTowers;
    [SerializeField, Tooltip("Exactly six unique tower definitions in persistent slots T1 through T6.")] private TurretDefinition[] initialDeck;
    public IReadOnlyList<StageDefinition> Stages => System.Array.AsReadOnly(stages);
    public IReadOnlyList<TurretDefinition> AvailableTowers => System.Array.AsReadOnly(availableTowers);
    public IReadOnlyList<TurretDefinition> InitialDeck => System.Array.AsReadOnly(initialDeck);
}
}
