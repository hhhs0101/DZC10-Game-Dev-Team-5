using System;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
public static class TowerLifetimeValidation {
    [MenuItem("Defense/Validate Tower Lifetime (Play Mode)")]
    public static void Run() {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode in an empty scene first.");
        float originalScale = Time.timeScale;
        var root = new GameObject("Tower lifetime validation");
        try {
            foreach (float cost in new[] { 2f, 3f, 4f })
                Check(Mathf.Abs(TowerLifetime.DurationForCost(cost) * TowerLifetime.LifetimeReferenceRegeneration - TowerLifetime.RechargeCycles * cost) < .001f, "Existing lifetime balance is preserved");
            Check(TowerLifetime.DurationForCost(0) == TowerLifetime.MinimumLifetime && TowerLifetime.DurationForCost(float.NaN) == TowerLifetime.MinimumLifetime, "Safe minimum");
            var flow = root.AddComponent<GameFlow>();
            var slotObject = new GameObject("Slot"); slotObject.transform.SetParent(root.transform);
            var slot = slotObject.AddComponent<TowerPlacementSlot>();
            var actor = new GameObject("Tower"); actor.transform.SetParent(slot.transform);
            var turret = actor.AddComponent<BasicTurret>();
            Check(slot.TryOccupy(turret), "Placement");
            turret.StartLifetime(slot, flow, 3);
            var timer = actor.GetComponent<TowerLifetime>();
            Check(timer.GetComponentInChildren<UnityEngine.UI.Text>().raycastTarget == false, "Countdown cannot intercept input");
            float initial = timer.Remaining;
            Check(timer.Advance(1) && Mathf.Abs(timer.Remaining - initial + 1) < .001f, "Playing advances");
            float remaining = timer.Remaining;
            timer.Advance(float.NaN); timer.Advance(float.PositiveInfinity); timer.Advance(-1);
            Check(timer.Remaining == remaining, "Invalid steps ignored");
            flow.TogglePause(); timer.Advance(10);
            Check(timer.Remaining == remaining && slot.IsOccupied, "Pause freezes lifetime");
            flow.TogglePause();
            int changes = 0; slot.Changed += () => changes++;
            Check(!timer.Advance(100) && timer.Remaining == 0 && !slot.IsOccupied && !actor.activeSelf, "Expiry releases slot and disables attacks");
            timer.Advance(100);
            Check(changes == 1, "Expiry is idempotent");
            var replacement = new GameObject("Replacement"); replacement.transform.SetParent(slot.transform);
            var second = replacement.AddComponent<BasicTurret>();
            Check(slot.TryOccupy(second), "Expired slot can be reused");
            slot.Release(turret);
            Check(slot.Occupant == second, "Old cleanup cannot release replacement");
            second.StartLifetime(slot, flow, 4);
            var secondTimer = replacement.GetComponent<TowerLifetime>();
            remaining = secondTimer.Remaining;
            flow.GameOver(); secondTimer.Advance(100);
            Check(secondTimer.Remaining == remaining && slot.Occupant == second, "Terminal state freezes lifetime");
            Debug.Log("Tower lifetime validation passed.");
        } finally {
            UnityEngine.Object.DestroyImmediate(root);
            Time.timeScale = originalScale;
        }
    }
    private static void Check(bool condition, string message) {
        if (!condition) throw new Exception("Tower lifetime validation failed: " + message);
    }
}
}
