using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Cards/Tower")]
public sealed class TowerCardDefinition : CardDefinition {
    [SerializeField] private TurretDefinition tower;
    public TurretDefinition Tower => tower;
    public override Sprite Icon => tower != null ? tower.Sprite : null;
    public override CardType Type => CardType.Tower;
}
}
