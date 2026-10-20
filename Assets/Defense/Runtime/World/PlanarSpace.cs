using UnityEngine;
namespace Defense {
// Pure coordinate operations. Every live battlefield chooses its own plane; old callers default to XZ.
public static class PlanarSpace {
    public static Vector2 Project(Vector3 world, WaypointPlane plane = WaypointPlane.XZ) => plane == WaypointPlane.XY ? new Vector2(world.x,world.y) : new Vector2(world.x,world.z);
    public static Vector3 World(Vector2 point,float depth=0,WaypointPlane plane=WaypointPlane.XZ) => plane == WaypointPlane.XY ? new Vector3(point.x,point.y,depth) : new Vector3(point.x,depth,point.y);
    public static float SqrDistance(Vector3 a,Vector3 b,WaypointPlane plane=WaypointPlane.XZ) => (Project(a,plane)-Project(b,plane)).sqrMagnitude;
}
}
