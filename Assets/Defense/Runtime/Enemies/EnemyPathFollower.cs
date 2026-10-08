using System;
using UnityEngine;
namespace Defense {
public sealed class EnemyPathFollower : MonoBehaviour {
    private WaypointPath path;
    public float BaseMovementSpeed { get; private set; }
    public MovementModifiers Modifiers { get; } = new MovementModifiers();
    public float EffectiveMovementSpeed => BaseMovementSpeed * Modifiers.Multiplier;
    public event Action Moved;
    private int next;
    private bool following;
    public event Action ReachedEnd;
    public void Initialize(WaypointPath route, float movementSpeed) {
        path = route; BaseMovementSpeed = movementSpeed; Modifiers.Clear(); next = 1;
        transform.position = path.GetPoint(0); following = true;
    }
    public void Stop() { following = false; }
    private void Update() { Advance(Time.deltaTime); }
    public void Advance(float deltaTime) {
        if (!following || deltaTime <= 0) return;
        transform.position = new Vector3(transform.position.x,path.transform.position.y,transform.position.z);
        float remaining = EffectiveMovementSpeed * deltaTime;
        // Carry unused movement across corners, including coincident waypoints.
        while (following && next < path.Count) {
            Vector3 destination = path.GetPoint(next);
            float distance = Vector3.Distance(transform.position, destination);
            if (distance > remaining) {
                transform.position = Vector3.MoveTowards(transform.position, destination, remaining);
                Moved?.Invoke(); return;
            }
            transform.position = destination; remaining -= distance; next++; Moved?.Invoke();
            if (next == path.Count) { following = false; ReachedEnd?.Invoke(); }
        }
    }
}
}
