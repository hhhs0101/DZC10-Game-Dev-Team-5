using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense {
[RequireComponent(typeof(SettingsUI))]
public sealed class MainMenuUI : MonoBehaviour {
    [SerializeField, Tooltip("Stages displayed in this order. Scenes must be in Build Settings.")] private StageDefinition[] stages;
    [SerializeField] private Canvas canvas;
    private GameObject menu;
    private GameObject selection;
    private SettingsUI settings;
    private void Start() {
        Time.timeScale = 1;
        menu = UiFactory.Panel("Main Menu", canvas.transform, new Color(.07f,.1f,.15f));
        UiFactory.Label(menu.transform, "2D DEFENSE", new Vector2(.5f,.5f), new Vector2(0,160), new Vector2(600,70), 42);
        UiFactory.Button(menu.transform, "Start Game", new Vector2(0,40), () => { menu.SetActive(false); selection.SetActive(true); });
        UiFactory.Button(menu.transform, "Settings", new Vector2(0,-30), () => { menu.SetActive(false); settings.Open(() => menu.SetActive(true)); });
        UiFactory.Button(menu.transform, "Exit", new Vector2(0,-100), Quit);
        selection = UiFactory.Panel("Stage Selection", canvas.transform, new Color(.07f,.1f,.15f));
        UiFactory.Label(selection.transform, "Select Stage", new Vector2(.5f,1), new Vector2(0,-70), new Vector2(600,70), 36);
        // A scrollable catalog supports additional stage definitions without rewriting navigation.
        var viewport = UiFactory.Rect("Stage List", selection.transform, new Vector2(.5f,.5f), new Vector2(0,10), new Vector2(400,400));
        viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(.07f,.1f,.15f);
        var scroll = viewport.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        var content = UiFactory.Rect("Content", viewport, new Vector2(.5f,1), Vector2.zero, new Vector2(380,Mathf.Max(400, stages.Length * 70)));
        content.pivot = new Vector2(.5f,1); scroll.content = content; scroll.viewport = viewport; scroll.horizontal = false;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
        for (int i=0; i<stages.Length; i++) {
            StageDefinition stage = stages[i];
            UiFactory.Button(content, stage.DisplayName, new Vector2(0,-40-i*70), () => SceneManager.LoadScene(stage.SceneName), new Vector2(.5f,1));
        }
        UiFactory.Button(selection.transform, "Back", new Vector2(0,60), () => { selection.SetActive(false); menu.SetActive(true); }, new Vector2(.5f,0));
        selection.SetActive(false);
        settings = GetComponent<SettingsUI>(); settings.Initialize(canvas.transform);
    }
    private static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
}
