using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Defense.Editor {
[InitializeOnLoad]
public static class TowerPlacementDragValidation {
    private const string Key = "Defense.TowerDrag.Validation";
    private static int checks;
    private static double started;

    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindAnyObjectByType<T>();
    private static GameplayHand Hand => Find<GameplayHand>();
    private static GameFlow Flow => Find<GameFlow>();
    private static TurretPlacementController Placement => Find<TurretPlacementController>();
    private static BattlefieldMap2D Map => Find<BattlefieldMap2D>();
    private static ElixirSystem Elixir => Find<ElixirSystem>();
    private static ResourceWallet Wallet => Find<ResourceWallet>();

    static TowerPlacementDragValidation() {
        if (SessionState.GetBool(Key, false)) Attach();
    }

    [MenuItem("Defense/Validation/Run Tower Drag Acceptance")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        SessionState.SetBool(Key + ".Background", Application.runInBackground);
        Application.runInBackground = true;
        EditorSceneManager.OpenScene(TopDownStage2Setup.ScenePath);
        SessionState.SetBool(Key, true);
        Attach();
        EditorApplication.isPlaying = true;
    }

    private static void Attach() {
        checks = 0;
        started = EditorApplication.timeSinceStartup;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    private static void OnLog(string m, string trace, LogType type) {
        if (trace.Contains("UnityEditor.Search.SearchDatabase") && !trace.Contains("Defense.")) return;
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, m + "\n" + trace);
    }

    private static void Check(bool pass, string message) {
        if (!pass) throw new Exception(message);
        checks++;
        Debug.Log("TOWER DRAG PASS: " + message);
    }

    private static Vector2 ScreenPoint(Vector3 worldPoint) => Camera.main.WorldToScreenPoint(worldPoint);

    private static void Verify() {
        Find<FixedIntervalSpawner>().enabled = false;
        foreach (var enemy in Find<EnemyRegistry>().Enemies.ToArray()) UnityEngine.Object.DestroyImmediate(enemy.gameObject);

        int towerIndex = Hand.Cycle.Hand.ToList().FindIndex(c => c.Definition is TowerCardDefinition);
        Check(towerIndex >= 0, "Hand contains at least one TowerCardDefinition");
        var towerCard = (TowerCardDefinition)Hand.Cycle.Hand[towerIndex].Definition;

        // 1. Select card and verify hover preview on unoccupied placement board
        Hand.Select(towerIndex);
        Check(Placement.Selected == towerCard.Tower, "Placement controller has selected turret");

        var slot = Map.PlacementSlots[0];
        Vector2 slotScreen = ScreenPoint(slot.PlacementPoint.position);
        Placement.ProcessPointer(slotScreen, false);

        Check(slot.IsHovered, "Slot is hovered when pointer moves over it");
        Check(Placement.PreviewObject != null && Placement.PreviewObject.activeSelf, "Ghost preview object is active on hover");
        Check(Placement.PreviewRenderer != null && Placement.PreviewRenderer.sprite != null, "Preview renderer has valid tower sprite");
        Check(Mathf.Approximately(Placement.PreviewRenderer.color.a, 0.62f), "Preview renderer is semi-transparent with alpha ~0.62");
        Check(Placement.RangeVisual != null && Placement.RangeVisual.IsVisible, "Range indicator is visible on hover");

        // 2. Move away from board onto floor: preview and range hide
        Vector2 floorScreen = ScreenPoint(Vector3.zero);
        Placement.ProcessPointer(floorScreen, false);
        Check(!slot.IsHovered, "Slot is no longer hovered when moving away");
        Check(Placement.PreviewObject != null && !Placement.PreviewObject.activeSelf, "Ghost preview is hidden when not hovering slot");
        Check(Placement.RangeVisual != null && !Placement.RangeVisual.IsVisible, "Range indicator is hidden when not hovering slot");

        Hand.CancelSelection();
        Check(Placement.Selected == null, "Selection cancelled");

        // 3. Test drag and drop installation via HandCardDrag
        var cardButton = GameObject.Find("Hand Card " + (towerIndex + 1));
        Check(cardButton != null, "Found Hand Card GameObject");
        var drag = cardButton.GetComponent<HandCardDrag>();
        Check(drag != null, "Hand Card has HandCardDrag component attached");

        Wallet.Initialize(10000);
        Elixir.Advance(100);

        var eventData = new PointerEventData(EventSystem.current) {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, cardButton.transform.position)
        };

        // Begin drag
        drag.OnBeginDrag(eventData);
        Check(drag.IsDragging, "HandCardDrag is now dragging");
        Check(Hand.SelectedCard != null && Hand.SelectedCard.Definition == towerCard, "Dragging card selects it in GameplayHand");

        // Drag over slot
        eventData.position = slotScreen;
        drag.OnDrag(eventData);
        Check(slot.IsHovered, "Dragging over slot sets slot hovered");
        Check(Placement.PreviewObject != null && Placement.PreviewObject.activeSelf, "Ghost preview is shown during drag over slot");
        Check(Placement.RangeVisual != null && Placement.RangeVisual.IsVisible, "Range visual is shown during drag over slot");

        // Drop on slot
        drag.OnEndDrag(eventData);
        Check(!drag.IsDragging, "Dragging ended");
        Check(slot.IsOccupied, "Dropping tower card installs tower onto slot");
        Check(slot.Occupant != null, "Slot occupant is non-null");
        Check(Placement.PreviewObject != null && !Placement.PreviewObject.activeSelf, "Ghost preview hidden after placement");
        Check(Placement.RangeVisual != null && !Placement.RangeVisual.IsVisible, "Range visual hidden after placement");
        Check(Hand.SelectedCard == null, "Card deselected after successful drop placement");

        // 4. Test dragging and dropping off-slot (cancels without placing)
        int nextTowerIndex = Hand.Cycle.Hand.ToList().FindIndex(c => c.Definition is TowerCardDefinition);
        if (nextTowerIndex >= 0) {
            var nextCardBtn = GameObject.Find("Hand Card " + (nextTowerIndex + 1));
            var nextDrag = nextCardBtn.GetComponent<HandCardDrag>();
            var offData = new PointerEventData(EventSystem.current) {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, nextCardBtn.transform.position)
            };
            nextDrag.OnBeginDrag(offData);
            Check(nextDrag.IsDragging, "Second drag started");
            offData.position = floorScreen;
            nextDrag.OnDrag(offData);
            nextDrag.OnEndDrag(offData);
            Check(!nextDrag.IsDragging, "Second drag ended");
            Check(Hand.SelectedCard == null, "Drop off slot cancels card selection without placing");
        }
    }

    private static void Tick() {
        if (!SessionState.GetBool(Key, false)) return;
        try {
            if (EditorApplication.timeSinceStartup - started > 45) throw new Exception("Tower drag validation timeout");
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || Find<TopDownInteractionSandbox>() == null || GameObject.Find("Hand Card 1") == null) return;
            Verify();
            Finish(true, checks + " tower drag/hover/drop assertions passed");
        } catch (Exception e) {
            Finish(false, e.ToString());
        }
    }

    private static void Finish(bool pass, string message) {
        if (!SessionState.GetBool(Key, false)) return;
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;
        Directory.CreateDirectory("Documentation/Report");
        File.WriteAllText("Documentation/Report/Tower_Drag_Validation_Result.txt", (pass ? "PASS: " : "FAIL: ") + message);
        if (pass) Debug.Log("TOWER_DRAG_PASSED " + message);
        else Debug.LogError("TOWER_DRAG_FAILED " + message);
        EditorApplication.isPlaying = false;
        Application.runInBackground = SessionState.GetBool(Key + ".Background", false);
        if (Application.isBatchMode) EditorApplication.Exit(pass ? 0 : 1);
    }
}
}
