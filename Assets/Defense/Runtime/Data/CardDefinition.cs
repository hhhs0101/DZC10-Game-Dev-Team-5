using UnityEngine;
namespace Defense {
public enum CardType { Tower, Skill }
public abstract class CardDefinition : ScriptableObject {
    [SerializeField] private string cardId;
    [SerializeField] private string displayName;
    [SerializeField, Min(0)] private float elixirCost;
    [SerializeField] private Color color = new Color(.18f,.26f,.34f);
    public string CardId => cardId;
    public string DisplayName => displayName;
    public float ElixirCost => elixirCost;
    public Color Color => color;
    public abstract CardType Type { get; }
    public string CostDescription => "Elixir: " + ElixirCost + (this is TowerCardDefinition tower ? " / Resources: " + tower.Tower.Cost : "");
}
}
