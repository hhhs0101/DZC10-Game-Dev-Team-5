using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Uses the same spawn-plan precedence as FixedIntervalSpawner.
public sealed class StageEnemyPreview : MonoBehaviour {
    private StageSelection selection;
    private GameCatalog catalog;
    private EnemyDefinitionCatalog enemies;
    private Text title;
    private RectTransform row;
    public void Initialize(GameCatalog data, StageSelection selected) {
        catalog = data; selection = selected;
        enemies = Resources.Load<EnemyDefinitionCatalog>(EnemyDefinitionCatalog.ResourcePath);
        var background = gameObject.AddComponent<Image>();
        background.color = new Color(.12f,.08f,.05f,.88f);
        background.raycastTarget = false;
        title = UiFactory.Label(transform,"",new Vector2(.5f,1),new Vector2(0,-19),new Vector2(900,30),22);
        title.color = new Color(1f,.92f,.65f);
        row = UiFactory.Rect("Enemy portraits",transform,new Vector2(.5f,.5f),new Vector2(0,-17),new Vector2(920,76));
        selection.Changed += Refresh;
        Refresh();
    }
    public static bool TryGetEnemies(StageDefinition stage, EnemyDefinitionCatalog enemyCatalog, out List<EnemyDefinition> roster) {
        roster = new List<EnemyDefinition>();
        if (stage == null) return false;
        if (stage.SpawnPlanJson != null) {
            if (!SpawnPlanLoader.TryLoad(stage.SpawnPlanJson,stage.DisplayName,enemyCatalog,out var plan,out var error)) return false;
            foreach (var entry in plan.Events) Add(roster,entry.Enemy);
        } else if (stage.SpawnSequence.Count > 0) {
            foreach (var enemy in stage.SpawnSequence) Add(roster,enemy);
        } else Add(roster,stage.Enemy);
        // Boss summons are real encounters too; keep each type only once.
        for (int i=0;i<roster.Count;i++) {
            var enemy = roster[i];
            if (enemy.Role != EnemyRole.Boss || enemy.SummonTypes == null) continue;
            foreach (var summon in enemy.SummonTypes)
                if (summon != null && summon.Role != EnemyRole.Boss && summon.Prefab != null) Add(roster,summon);
        }
        return roster.Count > 0;
    }
    private static void Add(List<EnemyDefinition> roster, EnemyDefinition enemy) {
        if (enemy != null && !roster.Contains(enemy)) roster.Add(enemy);
    }
    private void Refresh() {
        foreach (Transform child in row) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var stage = catalog.Stages[selection.SelectedStageIndex];
        title.text = stage.DisplayName + " - Enemies you'll face";
        if (!TryGetEnemies(stage,enemies,out var roster)) {
            UiFactory.Label(row,"Enemy preview unavailable",new Vector2(.5f,.5f),Vector2.zero,new Vector2(880,60),20);
            return;
        }
        float width = Mathf.Min(112f,920f/roster.Count);
        for (int i=0;i<roster.Count;i++) {
            var enemy = roster[i];
            var item = UiFactory.Rect(enemy.DisplayName,row,new Vector2(.5f,.5f),new Vector2((i-(roster.Count-1)*.5f)*width,0),new Vector2(width,76));
            var portrait = UiFactory.Rect("Portrait",item,new Vector2(.5f,.5f),new Vector2(0,18),new Vector2(36,36)).gameObject.AddComponent<Image>();
            portrait.sprite = enemy.Sprite;
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            portrait.enabled = enemy.Sprite != null;
            string displayName = enemy.DisplayName;
            int lastSpace = displayName.LastIndexOf(' ');
            if (displayName.Length > 14 && lastSpace > 0)
                displayName = displayName.Substring(0,lastSpace) + "\n" + displayName.Substring(lastSpace+1);
            var label = UiFactory.Label(item,displayName,new Vector2(.5f,.5f),new Vector2(0,-21),new Vector2(width-8,42),18);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.lineSpacing = .85f;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 18;
        }
    }
    private void OnDestroy() { if (selection != null) selection.Changed -= Refresh; }
}
}
