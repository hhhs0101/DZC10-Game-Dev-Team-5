using UnityEngine;
namespace Defense {
// Gameplay uses XZ. Height is presentation only.
public static class PlanarSpace {
    public static Vector2 Project(Vector3 world) => new Vector2(world.x,world.z);
    public static Vector3 World(Vector2 point,float height=0) => new Vector3(point.x,height,point.y);
    public static float SqrDistance(Vector3 a,Vector3 b) => (Project(a)-Project(b)).sqrMagnitude;
}
}
