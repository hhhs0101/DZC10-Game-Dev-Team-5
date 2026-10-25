using UnityEngine;
namespace Defense {
// Optional observer on the shared 2D prefab. Only the visual child changes.
[DisallowMultipleComponent, RequireComponent(typeof(Tower))]
public sealed class TurretSpriteView : MonoBehaviour {
    public enum AttackMotionStyle {
        Squeeze,
        ShakeHorizontal,
        RotateTilt
    }

    private const float MotionDuration = 0.28f;
    private const float MayoPulseInterval = 1.6f;

    [SerializeField] private SpriteRenderer appearance;
    private Tower turret;
    private Sprite originalSprite;
    private Color originalColor;
    private Vector3 originalScale, originalPosition;
    private Vector3 baseScale, basePosition;
    private AttackMotionStyle motionStyle;
    private float motionRemaining;
    private float tiltDirection = -1f;
    private float mayoPulseTimer;

    private void Awake() {
        turret = GetComponent<Tower>();
        originalSprite = appearance.sprite; originalColor = appearance.color;
        originalScale = appearance.transform.localScale; originalPosition = appearance.transform.localPosition;
        baseScale = originalScale; basePosition = originalPosition;
    }

    private void OnEnable() {
        turret.Initialized += Refresh;
        turret.Attacked += OnAttacked;
        Refresh(turret.Definition);
    }

    private void Refresh(TurretDefinition definition) {
        bool custom = definition != null && definition.Sprite != null;
        appearance.sprite = custom ? definition.Sprite : originalSprite;
        appearance.color = custom ? Color.white : originalColor;
        baseScale = custom ? new Vector3(definition.SpriteScale, definition.SpriteScale, 1f) : originalScale;
        basePosition = custom ? new Vector3(definition.SpriteOffset.x, definition.SpriteOffset.y, originalPosition.z) : originalPosition;
        motionStyle = ResolveMotionStyle(definition);
        motionRemaining = 0f;
        mayoPulseTimer = MayoPulseInterval * 0.5f;
        ResetTransform();
    }

    private static AttackMotionStyle ResolveMotionStyle(TurretDefinition definition) {
        if (definition == null) return AttackMotionStyle.Squeeze;
        string name = (definition.DisplayName ?? "").Trim().ToLowerInvariant();
        if (definition.TowerType == TowerType.Salt ||
            definition.TowerType == TowerType.BlackPepper ||
            name.Contains("salt") ||
            name.Contains("paprika") ||
            name.Contains("pepper")) {
            return AttackMotionStyle.ShakeHorizontal;
        }
        if (definition.TowerType == TowerType.Oil ||
            definition.TowerType == TowerType.HotSauce ||
            definition.TowerType == TowerType.SoySauce ||
            name.Contains("olive oil") ||
            name.Contains("oil") ||
            name.Contains("hot sauce") ||
            name.Contains("soy sauce") ||
            name.Contains("soya sauce") ||
            name.Contains("vinegar")) {
            return AttackMotionStyle.RotateTilt;
        }
        return AttackMotionStyle.Squeeze;
    }

    private void OnAttacked(Enemy target) {
        if (appearance == null) return;
        motionRemaining = MotionDuration;
        if (target != null) {
            float dx = target.transform.position.x - transform.position.x;
            tiltDirection = dx >= 0f ? -1f : 1f;
        } else {
            tiltDirection = -tiltDirection;
        }
    }

    private void Update() {
        if (appearance == null || turret == null || turret.Definition == null) return;
        if (!turret.AttackDisabled && turret.TowerType == TowerType.Mayonnaise && Time.deltaTime > 0f) {
            mayoPulseTimer -= Time.deltaTime;
            if (mayoPulseTimer <= 0f) {
                mayoPulseTimer = MayoPulseInterval;
                if (motionRemaining <= 0f) motionRemaining = MotionDuration;
            }
        }
        if (motionRemaining <= 0f) return;
        motionRemaining = Mathf.Max(0f, motionRemaining - Time.deltaTime);
        if (motionRemaining <= 0f) {
            ResetTransform();
            return;
        }
        float progress = 1f - (motionRemaining / MotionDuration);
        ApplyMotion(progress);
    }

    private void ApplyMotion(float t) {
        float envelope = Mathf.Sin(t * Mathf.PI);
        switch (motionStyle) {
            case AttackMotionStyle.ShakeHorizontal: {
                // Fast left-right shaker oscillation (3 full back-and-forth cycles).
                float wave = Mathf.Sin(t * Mathf.PI * 6f) * envelope;
                appearance.transform.localPosition = new Vector3(basePosition.x + wave * 0.16f, basePosition.y, basePosition.z);
                appearance.transform.localRotation = Quaternion.Euler(0f, 0f, -wave * 6f);
                appearance.transform.localScale = baseScale;
                break;
            }
            case AttackMotionStyle.RotateTilt: {
                // Pouring/tilting rotation toward the target and back.
                float angle = tiltDirection * 24f * envelope;
                appearance.transform.localPosition = basePosition;
                appearance.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                appearance.transform.localScale = baseScale;
                break;
            }
            default: {
                // Squeeze squash-and-stretch while keeping the bottle base planted.
                float squeeze;
                if (t < 0.55f) {
                    squeeze = Mathf.Sin((t / 0.55f) * Mathf.PI);
                } else {
                    squeeze = -0.35f * Mathf.Sin(((t - 0.55f) / 0.45f) * Mathf.PI);
                }
                float sx = 1f + 0.24f * squeeze;
                float sy = 1f - 0.22f * squeeze;
                appearance.transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
                appearance.transform.localPosition = new Vector3(basePosition.x, basePosition.y - (1f - sy) * 0.22f, basePosition.z);
                appearance.transform.localRotation = Quaternion.identity;
                break;
            }
        }
    }

    private void ResetTransform() {
        if (appearance == null) return;
        appearance.transform.localPosition = basePosition;
        appearance.transform.localRotation = Quaternion.identity;
        appearance.transform.localScale = baseScale;
    }

    private void OnDisable() {
        if (turret != null) {
            turret.Initialized -= Refresh;
            turret.Attacked -= OnAttacked;
        }
        ResetTransform();
    }
}
}
