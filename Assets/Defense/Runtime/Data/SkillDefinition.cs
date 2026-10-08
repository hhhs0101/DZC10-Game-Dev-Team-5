using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Skill Definition")]
public sealed class SkillDefinition : ScriptableObject {
    [SerializeField, Min(0)] private float damage = 40;
    [SerializeField, Min(.01f)] private float radius = 1.5f;
    [SerializeField, Min(0)] private float castingTime;
    [SerializeField, Min(0), Tooltip("Time after launch before the effect arrives; separate from pre-launch Casting Time.")] private float travelTime;
    [Header("Persistent Slow Area")]
    [SerializeField, Min(0)] private float activeDuration = 8;
    [SerializeField, Min(0), Tooltip("Presentation only; slowing ends before fading starts.")] private float fadeDuration = .5f;
    [SerializeField, Range(0,1), Tooltip("0.5 means half normal speed. Overlapping slows use the strongest source once.")] private float slowMultiplier = .5f;
    [SerializeField] private Skill effect;
    public float Damage => damage;
    public float Radius => radius;
    public float CastingTime => castingTime;
    public float TravelTime => travelTime;
    public float ActiveDuration => activeDuration;
    public float FadeDuration => fadeDuration;
    public float SlowMultiplier => slowMultiplier;
    public Skill Effect => effect;
}
}
