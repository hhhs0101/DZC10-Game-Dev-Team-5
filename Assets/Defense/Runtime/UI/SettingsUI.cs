using System;
using UnityEngine;
namespace Defense {
public sealed class SettingsUI : MonoBehaviour {
    private GameObject panel;
    private Action returnAction;
    private UnityEngine.UI.Text devLabel;
    public void Initialize(Transform canvas) {
        panel = UiFactory.Panel("Settings", canvas, new Color(.05f,.08f,.12f,.98f));
        UiFactory.Label(panel.transform, "Settings", new Vector2(.5f,.5f), new Vector2(0,110), new Vector2(500,60), 36);
        var devButton = UiFactory.Button(panel.transform, "", new Vector2(0,35), ToggleDevMode,
            size: new Vector2(440,52));
        devLabel = devButton.GetComponentInChildren<UnityEngine.UI.Text>();
        UiFactory.Label(panel.transform, "Enter any stage without unlocking it. Resets when the game restarts.",
            new Vector2(.5f,.5f), new Vector2(0,-20), new Vector2(740,48),18);
        RefreshDevLabel();
        UiFactory.Button(panel.transform, "Back", new Vector2(0,-80), Close);
        panel.SetActive(false);
    }
    private void ToggleDevMode() { PlayerSession.DevMode = !PlayerSession.DevMode; RefreshDevLabel(); }
    private void RefreshDevLabel() { devLabel.text = "Developer Mode: " + (PlayerSession.DevMode ? "ON" : "OFF"); }
    public void Open(Action onReturn) {
        RefreshDevLabel();
        returnAction = onReturn;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        GameAudio.PlayHomeBgm();
    }
    public void Hide() { panel.SetActive(false); returnAction = null; }
    private void Close() { Action callback = returnAction; Hide(); callback?.Invoke(); }
}
}
