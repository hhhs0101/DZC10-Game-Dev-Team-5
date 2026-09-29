using System;
using UnityEngine;
namespace Defense {
public sealed class SettingsUI : MonoBehaviour {
    private GameObject panel;
    private Action returnAction;
    public void Initialize(Transform canvas) {
        panel = UiFactory.Panel("Settings", canvas, new Color(.05f,.08f,.12f,.98f));
        UiFactory.Label(panel.transform, "Settings", new Vector2(.5f,.5f), new Vector2(0,110), new Vector2(500,60), 36);
        UiFactory.Label(panel.transform, "Settings will be available in a future version.", new Vector2(.5f,.5f), new Vector2(0,20), new Vector2(700,60));
        UiFactory.Button(panel.transform, "Back", new Vector2(0,-80), Close);
        panel.SetActive(false);
    }
    public void Open(Action onReturn) { returnAction = onReturn; panel.transform.SetAsLastSibling(); panel.SetActive(true); }
    public void Hide() { panel.SetActive(false); returnAction = null; }
    private void Close() { Action callback = returnAction; Hide(); callback?.Invoke(); }
}
}
