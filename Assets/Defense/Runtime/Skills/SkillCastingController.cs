using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class SkillCastingController : MonoBehaviour {
    [SerializeField, Tooltip("Bounds for isolated programmatic XY casts; mouse input requires a BattlefieldPointer2D.")] private Rect gameplayArea = new Rect(-9,-4.5f,18,9);
    [SerializeField, Tooltip("Optional future projectile launch point. Damage always uses the committed target.")] private Transform throwOrigin;
    [SerializeField, Tooltip("Required 2D world input.")] private BattlefieldPointer2D pointer2D;
    private WaypointPlane Plane => registry != null ? registry.Plane : WaypointPlane.XY;
    private float Depth => pointer2D != null ? pointer2D.Depth : 0;
    private GameplayHand hand;
    private GameFlow flow;
    private EnemyRegistry registry;
    private RuntimeCard aimingCard;
    private SkillAreaVisual indicator;
    private readonly List<PendingCast> pending = new List<PendingCast>();
    private sealed class PendingCast {
        public SkillDefinition Definition;
        public SkillCastContext Context;
        public float CastingRemaining;
        public float TravelRemaining;
    }
    public int PendingCastCount => pending.Count;
    public bool IsAiming => aimingCard != null;
    public Vector2 AimPosition { get; private set; }
    public float IndicatorRadius { get; private set; }
    public void Initialize(GameplayHand gameplayHand, GameFlow gameFlow, EnemyRegistry enemies, Camera camera) {
        hand = gameplayHand; flow = gameFlow; registry = enemies;
        flow.Changed += OnStateChanged; hand.Changed += OnSelectionChanged;
        var circle = new GameObject("Skill Aim Indicator"); circle.transform.SetParent(transform,false);
        indicator = circle.AddComponent<SkillAreaVisual>();
    }
    private void Update() {
        Advance(Time.deltaTime);
        ProcessPointer(Input.mousePosition,Input.GetMouseButtonDown(0),Input.GetMouseButtonUp(0));
    }
    // Both live input and integration tests use these screen-coordinate events.
    public CardUseResult ProcessPointer(Vector2 screenPosition, bool pressed, bool released) {
        if (!flow.IsPlaying) { CancelAim(); return CardUseResult.Cancelled; }
        bool valid = TryGetTabletopPoint(screenPosition,out var world);
        var position = PlanarSpace.Project(world,Plane);
        if (pressed && valid) BeginAim(position,false);
        if (IsAiming) {
            if (valid) MoveAim(position); else indicator.Hide();
            if (released) {
                if (!valid) { CancelAim(); return CardUseResult.Cancelled; }
                return ReleaseAim(position,false);
            }
        }
        return CardUseResult.Cancelled;
    }
    public bool TryGetTabletopPoint(Vector2 screenPosition, out Vector3 position) {
        position = default;
        return pointer2D != null && pointer2D.TryGetPoint(screenPosition,out position);
    }
    private bool IsValidPoint(Vector2 point) => pointer2D != null ? pointer2D.Map.PlayableArea.Contains(point) : gameplayArea.Contains(point);
    public bool BeginAim(Vector2 position, bool overUi) {
        if (!flow.IsPlaying || overUi || !IsValidPoint(position) || !(hand.SelectedCard?.Definition is SkillCardDefinition card) || card.Skill == null || card.Skill.Effect == null) return false;
        aimingCard = hand.SelectedCard; IndicatorRadius = card.Skill.Radius;
        MoveAim(position); return true;
    }
    public void MoveAim(Vector2 position) {
        if (!IsAiming) return;
        IndicatorRadius = ((SkillCardDefinition)aimingCard.Definition).Skill.Radius;
        AimPosition = position;
        indicator.ShowSkill(PlanarSpace.World(position,Depth,Plane),((SkillCardDefinition)aimingCard.Definition).Skill,new Color(1,1,1,.9f));
    }
    public CardUseResult ReleaseAim(Vector2 position, bool overUi) {
        if (!IsAiming) return CardUseResult.Cancelled;
        var card = aimingCard; CancelAim();
        if (!flow.IsPlaying || overUi || !IsValidPoint(position) || card != hand.SelectedCard) return CardUseResult.Cancelled;
        if (!hand.CanUse(card)) return CardUseResult.Failure;
        var definition = ((SkillCardDefinition)card.Definition).Skill;
        var context = new SkillCastContext(PlanarSpace.World(position,Depth,Plane),registry,flow,throwOrigin);
        var result = hand.CompleteUse(card,CardUseResult.Success);
        if (result != CardUseResult.Success) return result;
        if (definition.CastingTime <= 0 && definition.TravelTime <= 0) definition.Effect.Apply(definition,context);
        else pending.Add(new PendingCast { Definition = definition, Context = context,
            CastingRemaining = definition.CastingTime, TravelRemaining = definition.TravelTime });
        return result;
    }
    public void Advance(float deltaTime) {
        if (!flow.IsPlaying || deltaTime <= 0) return;
        for (int i=pending.Count-1;i>=0;i--) {
            var cast = pending[i];
            float beforeLaunch = Mathf.Min(deltaTime,cast.CastingRemaining);
            cast.CastingRemaining -= beforeLaunch;
            // A future projectile launches at the casting/travel boundary using Context.ThrowOrigin.
            cast.TravelRemaining = Mathf.Max(0,cast.TravelRemaining-(deltaTime-beforeLaunch));
            if (cast.CastingRemaining > 0 || cast.TravelRemaining > 0) continue;
            pending.RemoveAt(i); cast.Definition.Effect.Apply(cast.Definition,cast.Context);
        }
    }
    public void CancelAim() { aimingCard = null; if (indicator != null) indicator.Hide(); }
    private void OnSelectionChanged() { if (aimingCard != hand.SelectedCard) CancelAim(); }
    private void OnStateChanged(GameplayState state) { if (state != GameplayState.Playing) CancelAim(); if (flow.IsTerminal) pending.Clear(); }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelAim(); }
    private void OnDisable() => CancelAim();
    private void OnDestroy() {
        if (flow != null) flow.Changed -= OnStateChanged;
        if (hand != null) hand.Changed -= OnSelectionChanged;
    }
}
}
