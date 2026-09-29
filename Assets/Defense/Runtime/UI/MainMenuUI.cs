using UnityEngine;
namespace Defense {
[RequireComponent(typeof(SettingsUI))]
public sealed class MainMenuUI : MonoBehaviour {
    [SerializeField] private Canvas canvas;
    private GameObject menu, lobby, deckEditor;
    private SettingsUI settings;
    private void Start() {
        Time.timeScale = 1; PlayerSession.Ensure();
        menu = UiFactory.Panel("Start Screen",canvas.transform,new Color(.07f,.1f,.15f));
        lobby = UiFactory.Panel("Lobby",canvas.transform,new Color(.07f,.1f,.15f));
        deckEditor = UiFactory.Panel("Deck Editor",canvas.transform,new Color(.07f,.1f,.15f));
        UiFactory.Label(menu.transform,"2D DEFENSE",new Vector2(.5f,.5f),new Vector2(0,160),new Vector2(600,70),42);
        UiFactory.Button(menu.transform,"Start Game",new Vector2(0,40),()=>Show(lobby));
        UiFactory.Button(menu.transform,"Settings",new Vector2(0,-30),()=>OpenSettings(menu));
        UiFactory.Button(menu.transform,"Exit",new Vector2(0,-100),Quit);
        lobby.AddComponent<LobbyUI>().Initialize(()=>Show(deckEditor),()=>OpenSettings(lobby),()=>Show(menu));
        deckEditor.AddComponent<DeckEditorUI>().Initialize((RectTransform)canvas.transform,()=>Show(lobby),()=>OpenSettings(deckEditor));
        settings = GetComponent<SettingsUI>(); settings.Initialize(canvas.transform);
        Show(PlayerSession.ReturnToLobby ? lobby : menu);
    }
    private void Show(GameObject panel) { menu.SetActive(panel==menu); lobby.SetActive(panel==lobby); deckEditor.SetActive(panel==deckEditor); }
    private void OpenSettings(GameObject origin) { origin.SetActive(false); settings.Open(()=>Show(origin)); }
    private static void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
}
