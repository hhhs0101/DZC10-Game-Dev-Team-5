using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class SkillCastingController : MonoBehaviour {
    [SerializeField, Tooltip("Fallback bounds for programmatic planar casts; mouse input requires a Tabletop Surface.")] private Rect gameplayArea = new Rect(-9,-4.5f,18,9);
    [SerializeField] private TabletopSurface surface;
    [SerializeField, Tooltip("Optional future projectile launch point. Damage always uses the committed target.")] private Transform throwOrigin;
    private GameplayHand hand;
    private GameFlow flow;
    private EnemyRegistry registry;
    private Camera worldCamera;
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
        worldCamera = surface != null ? surface.WorldCamera : camera;
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
        var position = PlanarSpace.Project(world);
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
        if (surface == null || worldCamera == null || !worldCamera.pixelRect.Contains(screenPosition) ||
            GameplayPointer.IsOverUI(screenPosition)) return false;
        var ray = worldCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray,out var hit,worldCamera.farClipPlane,surface.SurfaceLayers,QueryTriggerInteraction.Ignore) ||
            hit.collider.GetComponentInParent<TabletopSurface>() != surface || hit.normal.y < .9f ||
            !surface.Contains(PlanarSpace.Project(hit.point))) return false;
        position = PlanarSpace.World(PlanarSpace.Project(hit.point),surface.Height);
        return true;
    }
    private bool IsValidPoint(Vector2 point) => surface != null ? surface.Contains(point) : gameplayArea.Contains(point);
    public bool BeginAim(Vector2 position, bool overUi) {
        if (!flow.IsPlaying || overUi || !IsValidPoint(position) || !(hand.SelectedCard?.Definition is SkillCardDefinition card) || card.Skill == null || card.Skill.Effect == null) return false;
        aimingCard = hand.SelectedCard; IndicatorRadius = card.Skill.Radius;
        MoveAim(position); return true;
    }
    public void MoveAim(Vector2 position) {
        if (!IsAiming) return;
        IndicatorRadius = ((SkillCardDefinition)aimingCard.Definition).Skill.Radius;
        AimPosition = position;
        indicator.Show(PlanarSpace.World(position,surface != null ? surface.Height : 0),IndicatorRadius,new Color(1,.75f,.15f,.9f));
    }
    public CardUseResult ReleaseAim(Vector2 position, bool overUi) {
        if (!IsAiming) return CardUseResult.Cancelled;
        var card = aimingCard; CancelAim();
        if (!flow.IsPlaying || overUi || !IsValidPoint(position) || card != hand.SelectedCard) return CardUseResult.Cancelled;
        if (!hand.CanUse(card)) return CardUseResult.Failure;
        var definition = ((SkillCardDefinition)card.Definition).Skill;
        var context = new SkillCastContext(PlanarSpace.World(position,surface != null ? surface.Height : 0),registry,flow,throwOrigin);
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
    private void OnStateChanged(GameplayState state) { if (state != GameplayState.Playing) CancelAim(); if (state == GameplayState.GameOver) pending.Clear(); }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelAim(); }
    private void OnDisable() => CancelAim();
    private void OnDestroy() {
        if (flow != null) flow.Changed -= OnStateChanged;
        if (hand != null) hand.Changed -= OnSelectionChanged;
    }
}
}
