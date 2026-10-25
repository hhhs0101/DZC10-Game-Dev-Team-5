using UnityEngine;
namespace Defense {
// One read-only inspection owner. Input arrives through the existing placement pointer boundary.
[RequireComponent(typeof(GameplayHand),typeof(GameFlow),typeof(BattlefieldPointer2D))]
public sealed class PlacedTowerInspectionController : MonoBehaviour {
    [SerializeField] private GameplayHand hand;
    [SerializeField] private GameFlow flow;
    [SerializeField] private BattlefieldPointer2D pointer2D;
    [SerializeField] private SkillAreaVisual rangeIndicator;
    [SerializeField] private Color rangeColor = new Color(.15f,.9f,1,.9f);
    public Tower InspectedTower { get; private set; }
    private void Awake() {
        if (hand == null) hand = GetComponent<GameplayHand>();
        if (flow == null) flow = GetComponent<GameFlow>();
        if (pointer2D == null) pointer2D = GetComponent<BattlefieldPointer2D>();
    }
    private void OnEnable() {
        hand.Changed += ClearInspection; flow.Changed += OnStateChanged;
        ClearInspection();
    }
    // Never consumes a Card action. Its presence leaves the existing placement/cast route intact.
    public bool ProcessPointer(Vector2 screenPosition, bool pressed) {
        if (!pressed) return false;
        if (!isActiveAndEnabled || !flow.IsPlaying || hand.SelectedCard != null ||
            !pointer2D.TryGetPoint(screenPosition,out var world)) {
            ClearInspection(); return false;
        }
        var tower = pointer2D.GetPlacedTower(world);
        if (tower == null || tower == InspectedTower) { ClearInspection(); return false; }
        return InspectTower(tower);
    }
    public bool InspectTower(Tower tower) {
        if (!isActiveAndEnabled || !flow.IsPlaying || hand.SelectedCard != null ||
            tower == null || !tower.isActiveAndEnabled || tower.Definition == null ||
            tower.gameObject.scene != gameObject.scene) { ClearInspection(); return false; }
        InspectedTower = tower; RefreshIndicator(); return InspectedTower != null;
    }
    public void ClearInspection() {
        InspectedTower = null;
        if (rangeIndicator != null) rangeIndicator.Hide();
    }
    private void LateUpdate() => RefreshIndicator();
    public void RefreshIndicator() {
        if (rangeIndicator == null || !flow.IsPlaying || hand.SelectedCard != null || InspectedTower == null ||
            !InspectedTower.isActiveAndEnabled || InspectedTower.EffectiveAttackRange <= 0) {
            ClearInspection(); return;
        }
        rangeIndicator.Show(InspectedTower.transform.position,InspectedTower.EffectiveAttackRange,rangeColor,WaypointPlane.XY);
    }
    private void OnStateChanged(GameplayState state) { if (state != GameplayState.Playing) ClearInspection(); }
    private void OnDisable() {
        if (hand != null) hand.Changed -= ClearInspection;
        if (flow != null) flow.Changed -= OnStateChanged;
        ClearInspection();
    }
}
}
