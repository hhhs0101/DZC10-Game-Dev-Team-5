using System;
using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public sealed class SettingsUI : MonoBehaviour {
    private GameObject panel;
    private Action returnAction;
    private Text devLabel;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private Text bgmValueLabel;
    private Text sfxValueLabel;

    public void Initialize(Transform canvas) {
        panel = UiFactory.Panel("Settings", canvas, new Color(.05f,.08f,.12f,.98f));
        UiFactory.Label(panel.transform, "Settings", new Vector2(.5f,.5f), new Vector2(0,180), new Vector2(500,60), 36);

        bgmSlider = CreateVolumeRow(panel.transform, "BGM Volume", new Vector2(0,105), GameAudio.BgmVolume, out bgmValueLabel, value => {
            GameAudio.BgmVolume = value;
            UpdateVolumeLabels();
        });

        sfxSlider = CreateVolumeRow(panel.transform, "SFX Volume", new Vector2(0,40), GameAudio.SfxVolume, out sfxValueLabel, value => {
            GameAudio.SfxVolume = value;
            UpdateVolumeLabels();
        });

        var devButton = UiFactory.Button(panel.transform, "", new Vector2(0,-35), ToggleDevMode,
            size: new Vector2(440,52));
        devLabel = devButton.GetComponentInChildren<Text>();
        UiFactory.Label(panel.transform, "Enter any stage without unlocking it. Resets when the game restarts.",
            new Vector2(.5f,.5f), new Vector2(0,-90), new Vector2(740,48),18);
        RefreshDevLabel();
        UpdateVolumeLabels();
        UiFactory.Button(panel.transform, "Back", new Vector2(0,-155), Close);
        panel.SetActive(false);
    }

    private static Slider CreateVolumeRow(Transform parent, string title, Vector2 position, float initialValue, out Text valueLabel, UnityEngine.Events.UnityAction<float> onChanged) {
        var row = UiFactory.Rect(title + " Row", parent, new Vector2(.5f,.5f), position, new Vector2(520,46));
        var titleLabel = UiFactory.Label(row, title, new Vector2(0,.5f), new Vector2(75,0), new Vector2(150,40), 22);
        titleLabel.alignment = TextAnchor.MiddleLeft;

        var sliderRect = UiFactory.Rect(title + " Slider", row, new Vector2(.5f,.5f), new Vector2(30,0), new Vector2(240,24));
        var bg = UiFactory.Rect("Background", sliderRect, Vector2.zero, Vector2.zero, Vector2.zero);
        bg.anchorMin = new Vector2(0,.25f); bg.anchorMax = new Vector2(1,.75f); bg.offsetMin = bg.offsetMax = Vector2.zero;
        bg.gameObject.AddComponent<Image>().color = new Color(.18f,.24f,.32f,1f);

        var fillArea = UiFactory.Rect("Fill Area", sliderRect, Vector2.zero, Vector2.zero, Vector2.zero);
        fillArea.anchorMin = new Vector2(0,.25f); fillArea.anchorMax = new Vector2(1,.75f); fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;
        var fill = UiFactory.Rect("Fill", fillArea, Vector2.zero, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = new Color(1f,.84f,.12f,1f);

        var handleSlideArea = UiFactory.Rect("Handle Slide Area", sliderRect, Vector2.zero, Vector2.zero, Vector2.zero);
        handleSlideArea.anchorMin = Vector2.zero; handleSlideArea.anchorMax = Vector2.one; handleSlideArea.offsetMin = handleSlideArea.offsetMax = Vector2.zero;
        var handle = UiFactory.Rect("Handle", handleSlideArea, new Vector2(.5f,.5f), Vector2.zero, new Vector2(18,28));
        var handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;

        var slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.SetValueWithoutNotify(initialValue);
        slider.onValueChanged.AddListener(onChanged);

        valueLabel = UiFactory.Label(row, "", new Vector2(1,.5f), new Vector2(-40,0), new Vector2(80,40), 20);
        valueLabel.alignment = TextAnchor.MiddleRight;
        return slider;
    }

    private void UpdateVolumeLabels() {
        if (bgmValueLabel != null) bgmValueLabel.text = Mathf.RoundToInt(GameAudio.BgmVolume * 100f) + "%";
        if (sfxValueLabel != null) sfxValueLabel.text = Mathf.RoundToInt(GameAudio.SfxVolume * 100f) + "%";
    }

    private void ToggleDevMode() { PlayerSession.DevMode = !PlayerSession.DevMode; RefreshDevLabel(); }
    private void RefreshDevLabel() { devLabel.text = "Developer Mode: " + (PlayerSession.DevMode ? "ON" : "OFF"); }
    public void Open(Action onReturn) {
        RefreshDevLabel();
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(GameAudio.BgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameAudio.SfxVolume);
        UpdateVolumeLabels();
        returnAction = onReturn;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        GameAudio.PlayHomeBgm();
    }
    public void Hide() { panel.SetActive(false); returnAction = null; }
    private void Close() { Action callback = returnAction; Hide(); callback?.Invoke(); }
}
}
