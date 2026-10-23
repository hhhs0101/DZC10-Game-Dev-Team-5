using UnityEngine;
namespace Defense {
public enum CardType { Tower, Skill }
public abstract class CardDefinition : ScriptableObject {
    [SerializeField] private string cardId;
    [SerializeField] private string displayName;
    [SerializeField, Min(0)] private float elixirCost;
    [SerializeField] private Color color = new Color(.18f,.26f,.34f);
    [SerializeField] private Sprite cardSprite;
    public string CardId => cardId;
    public string DisplayName => displayName;
    public float ElixirCost => elixirCost;
    public Color Color => color;
    public virtual Sprite Icon => null;
    public Sprite CardSprite => cardSprite != null ? cardSprite : Icon;
    public abstract CardType Type { get; }
    public string CostDescription => "Energy: " + ElixirCost + (this is TowerCardDefinition tower && tower.Tower != null ? "\nCost: " + tower.Tower.Cost : "");
}
}
