using UnityEngine;
namespace Defense {
[CreateAssetMenu(menuName="Defense/Skill Range Profile")]
public sealed class SkillRangeProfile : ScriptableObject {
    [SerializeField] private Sprite markerSprite;
    [SerializeField, Tooltip("Measured bright-rim rectangle in original texture pixels, bottom-left origin. Outer glow is decorative.")]
    private Rect brightRimPixels = new Rect(128,176,1518,400);
    [SerializeField, Min(.001f), Tooltip("Gameplay vertical/horizontal semi-axis ratio. The same ratio scales the visible rim.")]
    private float ellipseAspectRatio = 400f/1518f;
    public Sprite MarkerSprite => markerSprite;
    public Rect BrightRimPixels => brightRimPixels;
    public float AspectRatio => ellipseAspectRatio;
    public EllipseFootprint CreateFootprint(Vector2 center,float radius) => new EllipseFootprint(center,radius,radius*AspectRatio);
    public Vector2 VisualScale(EllipseFootprint footprint) {
        if(markerSprite==null || brightRimPixels.width<=0 || brightRimPixels.height<=0 || !footprint.IsValid)
            throw new System.InvalidOperationException("Skill marker requires a Sprite, valid bright-rim calibration and positive finite ellipse radii.");
        return new Vector2(2*footprint.RadiusX*markerSprite.pixelsPerUnit/brightRimPixels.width,
            2*footprint.RadiusY*markerSprite.pixelsPerUnit/brightRimPixels.height);
    }
    // Move only the artwork, never the committed impact point. Supports an ordinary center pivot.
    public Vector2 VisualOffset(EllipseFootprint footprint) => Vector2.Scale(
        (brightRimPixels.center-markerSprite.rect.position-markerSprite.pivot)/markerSprite.pixelsPerUnit,VisualScale(footprint));
}
}
