using System;
using UnityEngine;
namespace Defense {
public sealed class EnemyPathFollower : MonoBehaviour {
    private WaypointPath path;
    private WaypointPath continuationPath;
    private float continuationDelay;
    private float continuationTimer;
    private bool waitingForContinuation;
    public float BaseMovementSpeed { get; private set; }
    public MovementModifiers Modifiers { get; } = new MovementModifiers();
    public float EffectiveMovementSpeed => BaseMovementSpeed * Modifiers.Multiplier;
    public bool IsInTunnel => waitingForContinuation;
    public event Action Moved;
    public event Action EnteredTunnel;
    public event Action ExitedTunnel;
    private int next;
    private bool following;
    public event Action ReachedEnd;
    public void Initialize(WaypointPath route, float movementSpeed) {
        path = route; BaseMovementSpeed = movementSpeed; Modifiers.Clear(); next = 1;
        continuationPath = null; continuationDelay = 0f; continuationTimer = 0f; waitingForContinuation = false;
        transform.position = path.GetPoint(0); following = true;
    }
    public void SetContinuation(WaypointPath nextRoute, float delaySeconds) {
        continuationPath = nextRoute != null && nextRoute.Count >= 2 ? nextRoute : null;
        continuationDelay = Mathf.Max(0f, delaySeconds);
    }
    public WaypointPath Route => path;
    public int NextWaypoint => next;
    public void ContinueFrom(EnemyPathFollower source) {
        path = source.path; next = source.next; transform.position = source.transform.position;
        continuationPath = source.continuationPath;
        continuationDelay = source.continuationDelay;
        continuationTimer = source.continuationTimer;
        waitingForContinuation = source.waitingForContinuation;
        following = waitingForContinuation || next < path.Count;
    }
    public void Stop() { following = false; waitingForContinuation = false; }
    private void Update() { Advance(Time.deltaTime); }
    public void Advance(float deltaTime) {
        if (!following || deltaTime <= 0) return;
        if (waitingForContinuation) {
            continuationTimer -= deltaTime;
            if (continuationTimer > 0f) return;
            float leftover = -continuationTimer;
            path = continuationPath;
            continuationPath = null;
            waitingForContinuation = false;
            next = 1;
            transform.position = path.GetPoint(0);
            ExitedTunnel?.Invoke();
            Moved?.Invoke();
            deltaTime = leftover;
            if (deltaTime <= 0f) return;
        }
        transform.position = path.Constrain(transform.position);
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
            if (next == path.Count) {
                if (continuationPath != null && continuationPath.Count >= 2) {
                    if (continuationDelay > 0f) {
                        waitingForContinuation = true;
                        continuationTimer = continuationDelay;
                        EnteredTunnel?.Invoke();
                        return;
                    }
                    path = continuationPath;
                    continuationPath = null;
                    next = 1;
                    transform.position = path.GetPoint(0);
                    ExitedTunnel?.Invoke();
                    Moved?.Invoke();
                    continue;
                }
                following = false; ReachedEnd?.Invoke();
            }
        }
    }
}
}
