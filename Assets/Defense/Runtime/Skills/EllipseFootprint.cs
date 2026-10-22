using UnityEngine;
namespace Defense {
// Axis-aligned geometry in logical gameplay coordinates, independent from sprite alpha or colliders.
public readonly struct EllipseFootprint {
    public Vector2 Center { get; }
    public float RadiusX { get; }
    public float RadiusY { get; }
    public bool IsValid => Finite(Center.x) && Finite(Center.y) && Finite(RadiusX) && Finite(RadiusY) && RadiusX>0 && RadiusY>0;
    public EllipseFootprint(Vector2 center,float radiusX,float radiusY) {Center=center;RadiusX=radiusX;RadiusY=radiusY;}
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    public bool Contains(Vector2 point) {
        if(!IsValid)return false;
        float x=(point.x-Center.x)/RadiusX,y=(point.y-Center.y)/RadiusY;
        // Include the rim despite float cancellation at small radii away from world origin.
        // This tolerance follows coordinate precision, not the decorative glow or texture alpha.
        float magnitude=Mathf.Max(1f,Mathf.Max(Mathf.Abs(Center.x),Mathf.Abs(Center.y)));
        float tolerance=1e-6f+4e-7f*magnitude/Mathf.Min(RadiusX,RadiusY);
        return x*x+y*y<=1f+tolerance;
    }
    public Vector2 Boundary(float radians) => Center+new Vector2(Mathf.Cos(radians)*RadiusX,Mathf.Sin(radians)*RadiusY);
}
}
