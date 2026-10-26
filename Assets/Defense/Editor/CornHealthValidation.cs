using System;
using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
public static class CornHealthValidation {
    [MenuItem("Defense/Validate Corn Health Layers")]
    public static void Run() {
        var probe = new GameObject("Corn health validation");
        try {
            var health = probe.AddComponent<EnemyHealth>();
            int deaths = 0;
            health.Depleted += () => deaths++;
            health.Initialize(20, 5);
            health.ReceiveDamage(33);
            Check(health.Current == 80, "Overkill must remove only one layer.");
            health.ReceiveDamage(7);
            Check(health.Current == 73, "Partial damage must stay on the current layer.");
            health.ReceiveDamage(100);
            Check(health.Current == 60, "Overkill on a wounded layer must not spill over.");
            health.ReceiveDamage(100);
            health.ReceiveDamage(100);
            Check(health.Current == 20 && deaths == 0, "Last layer must keep Corn alive.");
            health.ReceiveDamage(100);
            health.ReceiveDamage(100);
            Check(health.Current == 0 && deaths == 1, "Death must fire exactly once.");
            health.Initialize(20, 5);
            for (int i = 1; i <= 5; i++) {
                health.ReceiveDamage(1000);
                Check(health.Current == (5 - i) * 20, "Each huge hit must remove one layer.");
            }
            health.Initialize(100);
            health.ReceiveDamage(33);
            Check(health.Current == 67, "Ordinary enemy damage must remain unchanged.");
            health.ReceiveDamage(100);
            Check(health.Current == 0, "Ordinary enemies must still die from overkill.");
            health.Initialize(20, 5);
            health.ReceiveSplashDamage(6);
            for (int i = 0; i < 5; i++)
                Check(health.GetLayerHealth(i) == 14, "Splash must damage all five targets equally.");
            health.ReceiveDamage(100);
            Check(health.GetLayerHealth(0) == 0 && health.Current == 56,
                "A direct hit after splash must remove only one wounded target.");
            health.ReceiveDamage(5);
            health.ReceiveSplashDamage(10);
            Check(health.GetLayerHealth(0) == 0 && health.GetLayerHealth(1) == 0 && health.Current == 12,
                "Splash must keep dead targets dead and damage each survivor independently.");
            for (int i = 2; i < 5; i++)
                Check(health.GetLayerHealth(i) == 4, "Surviving bars must report individual health.");
            int beforeSplashDeath = deaths;
            health.ReceiveSplashDamage(100);
            health.ReceiveSplashDamage(100);
            Check(health.Current == 0 && deaths == beforeSplashDeath + 1,
                "Simultaneous layer deaths must emit exactly one Corn death.");
            health.Initialize(100);
            health.ReceiveSplashDamage(6);
            Check(health.Current == 94, "Splash must still hit ordinary enemies only once.");
            Debug.Log("CORN HEALTH VALIDATION PASSED");
        } finally { UnityEngine.Object.DestroyImmediate(probe); }
    }
    private static void Check(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
}
}
