using System.Collections.Generic;
using UnityEngine;
namespace Defense {
public sealed class SkillCastingController : MonoBehaviour {
    [SerializeField, Tooltip("Valid world coordinates for skill casts. UI always cancels.")] private Rect gameplayArea = new Rect(-9,-4.5f,18,9);
    private GameplayHand hand;
    private GameFlow flow;
    private EnemyRegistry registry;
    private Camera worldCamera;
    private RuntimeCard aimingCard;
    private LineRenderer indicator;
    private Material indicatorMaterial;
    private readonly List<PendingCast> pending = new List<PendingCast>();
    private sealed class PendingCast { public SkillDefinition Definition; public Vector2 Position; public float Remaining; }
    public bool IsAiming => aimingCard != null;
    public Vector2 AimPosition { get; private set; }
    public float IndicatorRadius { get; private set; }
    public void Initialize(GameplayHand gameplayHand, GameFlow gameFlow, EnemyRegistry enemies, Camera camera) {
        hand = gameplayHand; flow = gameFlow; registry = enemies; worldCamera = camera;
        flow.Changed += OnStateChanged; hand.Changed += OnSelectionChanged;
        var circle = new GameObject("Skill Aim Indicator"); circle.transform.SetParent(transform,false);
        indicator = circle.AddComponent<LineRenderer>(); indicator.useWorldSpace = true;
        indicator.loop = true; indicator.positionCount = 64; indicator.widthMultiplier = .04f;
        indicatorMaterial = new Material(Shader.Find("Sprites/Default")); indicator.sharedMaterial = indicatorMaterial;
        indicator.startColor = indicator.endColor = new Color(1,.75f,.15f,.9f); indicator.sortingOrder = 30; indicator.enabled = false;
    }
    private void Update() {
        Advance(Time.deltaTime);
        if (!flow.IsPlaying) { CancelAim(); return; }
        Vector2 position = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        bool overUi = GameplayPointer.IsOverUI(Input.mousePosition);
        if (Input.GetMouseButtonDown(0)) BeginAim(position,overUi);
        if (IsAiming && Input.GetMouseButton(0)) MoveAim(position);
        if (IsAiming && Input.GetMouseButtonUp(0)) ReleaseAim(position,overUi);
    }
    public bool BeginAim(Vector2 position, bool overUi) {
        if (!flow.IsPlaying || overUi || !gameplayArea.Contains(position) || !(hand.SelectedCard?.Definition is SkillCardDefinition card) || card.Skill == null || card.Skill.Effect == null) return false;
        aimingCard = hand.SelectedCard; IndicatorRadius = card.Skill.Radius;
        indicator.enabled = true; MoveAim(position); return true;
    }
    public void MoveAim(Vector2 position) {
        if (!IsAiming) return;
        AimPosition = position;
        for (int i=0;i<indicator.positionCount;i++) {
            float angle = i*Mathf.PI*2/indicator.positionCount;
            indicator.SetPosition(i,new Vector3(position.x+Mathf.Cos(angle)*IndicatorRadius,position.y+Mathf.Sin(angle)*IndicatorRadius,-.2f));
        }
    }
    public CardUseResult ReleaseAim(Vector2 position, bool overUi) {
        if (!IsAiming) return CardUseResult.Cancelled;
        var card = aimingCard; CancelAim();
        if (!flow.IsPlaying || overUi || !gameplayArea.Contains(position) || card != hand.SelectedCard) return CardUseResult.Cancelled;
        if (!hand.CanUse(card)) return CardUseResult.Failure;
        var definition = ((SkillCardDefinition)card.Definition).Skill;
        var result = hand.CompleteUse(card,CardUseResult.Success);
        if (result != CardUseResult.Success) return result;
        if (definition.CastingTime <= 0) definition.Effect.Apply(definition,position,registry);
        else pending.Add(new PendingCast { Definition = definition, Position = position, Remaining = definition.CastingTime });
        return result;
    }
    public void Advance(float deltaTime) {
        if (!flow.IsPlaying || deltaTime <= 0) return;
        for (int i=pending.Count-1;i>=0;i--) {
            var cast = pending[i]; cast.Remaining -= deltaTime;
            if (cast.Remaining > 0) continue;
            pending.RemoveAt(i); cast.Definition.Effect.Apply(cast.Definition,cast.Position,registry);
        }
    }
    public void CancelAim() { aimingCard = null; if (indicator != null) indicator.enabled = false; }
    private void OnSelectionChanged() { if (aimingCard != hand.SelectedCard) CancelAim(); }
    private void OnStateChanged(GameplayState state) { if (state != GameplayState.Playing) CancelAim(); if (state == GameplayState.GameOver) pending.Clear(); }
    private void OnDisable() => CancelAim();
    private void OnDestroy() {
        if (flow != null) flow.Changed -= OnStateChanged;
        if (hand != null) hand.Changed -= OnSelectionChanged;
        if (indicatorMaterial != null) Destroy(indicatorMaterial);
    }
}
}
