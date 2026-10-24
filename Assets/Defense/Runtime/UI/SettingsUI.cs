using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace Defense {
public sealed class SettingsUI : MonoBehaviour {
    private const string SettingsResourcePrefix = "UI/Settings/";
    private const float MuteEpsilon = 0.0001f;
    private static readonly Color HeaderTextColor = new Color(0.28f, 0.12f, 0.04f, 1f);
    private static readonly Color BodyTextColor = new Color(0.32f, 0.15f, 0.05f, 1f);
    private static readonly Color WoodButtonColor = new Color(0.52f, 0.24f, 0.09f, 1f);

    private GameObject panel;
    private Action returnAction;
    private Text devLabel;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private Text bgmValueLabel;
    private Text sfxValueLabel;
    private Image bgmIconImage;
    private Image sfxIconImage;
    private Image bgmFillImage;
    private Image sfxFillImage;
    private Sprite musicIconSprite;
    private Sprite musicMutedIconSprite;
    private Sprite sfxIconSprite;
    private Sprite sfxMutedIconSprite;
    private float lastUnmutedBgmVolume = 0.7f;
    private float lastUnmutedSfxVolume = 1f;

    public void Initialize(Transform canvas) {
        panel = UiFactory.Panel("Settings", canvas, new Color(.05f, .08f, .12f, .78f));

        var popupSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "settings_popup_panel");
        musicIconSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "music_icon");
        musicMutedIconSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "music_icon_muted");
        sfxIconSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "sfx_icon");
        if (sfxIconSprite == null) sfxIconSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "sound_effects_icon");
        sfxMutedIconSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "sfx_icon_muted");
        if (sfxMutedIconSprite == null) sfxMutedIconSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "sound_effects_icon_muted");

        var trackSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "slider_track");
        var fillSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "slider_fill");
        var handleSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "slider_handle");
        var handlePressedSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "slider_handle_pressed");
        var backNormalSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "back_normal");
        var backHoverSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "back_hover");
        var backPressedSprite = Resources.Load<Sprite>(SettingsResourcePrefix + "back_pressed");

        if (GameAudio.BgmVolume > MuteEpsilon) lastUnmutedBgmVolume = GameAudio.BgmVolume;
        if (GameAudio.SfxVolume > MuteEpsilon) lastUnmutedSfxVolume = GameAudio.SfxVolume;

        var popupRect = UiFactory.Rect("Settings Popup Panel", panel.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(960f, 540f));
        var popupImage = popupRect.gameObject.AddComponent<Image>();
        if (popupSprite != null) {
            popupImage.sprite = popupSprite;
            popupImage.preserveAspect = true;
            popupImage.color = Color.white;
        } else {
            popupImage.color = new Color(.96f, .89f, .78f, 1f);
        }
        popupImage.raycastTarget = false;

        var headerLabel = UiFactory.Label(panel.transform, "Settings", new Vector2(.5f, .5f), new Vector2(0f, 210f), new Vector2(500f, 60f), 38);
        headerLabel.color = HeaderTextColor;
        headerLabel.fontStyle = FontStyle.Bold;

        bgmSlider = CreateVolumeRow(
            panel.transform,
            "BGM Volume",
            new Vector2(0f, 92f),
            musicIconSprite,
            trackSprite,
            fillSprite,
            handleSprite,
            handlePressedSprite,
            GameAudio.BgmVolume,
            ToggleBgmMute,
            out bgmIconImage,
            out bgmFillImage,
            out bgmValueLabel,
            value => {
                if (value > MuteEpsilon) lastUnmutedBgmVolume = value;
                GameAudio.BgmVolume = value;
                UpdateVolumeVisuals();
            });

        sfxSlider = CreateVolumeRow(
            panel.transform,
            "SFX Volume",
            new Vector2(0f, 14f),
            sfxIconSprite,
            trackSprite,
            fillSprite,
            handleSprite,
            handlePressedSprite,
            GameAudio.SfxVolume,
            ToggleSfxMute,
            out sfxIconImage,
            out sfxFillImage,
            out sfxValueLabel,
            value => {
                if (value > MuteEpsilon) lastUnmutedSfxVolume = value;
                GameAudio.SfxVolume = value;
                UpdateVolumeVisuals();
            });

        var devButton = UiFactory.Button(panel.transform, "", new Vector2(0f, -58f), ToggleDevMode,
            size: new Vector2(420f, 46f));
        devButton.image.color = WoodButtonColor;
        devLabel = devButton.GetComponentInChildren<Text>();

        var infoLabel = UiFactory.Label(panel.transform, "Enter any stage without unlocking it. Resets when the game restarts.",
            new Vector2(.5f, .5f), new Vector2(0f, -100f), new Vector2(740f, 36f), 18);
        infoLabel.color = BodyTextColor;

        RefreshDevLabel();
        UpdateVolumeVisuals();

        var backButton = UiFactory.Button(panel.transform, "Back", new Vector2(0f, -148f), Close,
            size: new Vector2(200f, 64f));
        if (backNormalSprite != null) {
            backButton.image.sprite = backNormalSprite;
            backButton.image.color = Color.white;
            backButton.image.preserveAspect = true;
            backButton.transition = Selectable.Transition.SpriteSwap;
            backButton.spriteState = new SpriteState {
                highlightedSprite = backHoverSprite != null ? backHoverSprite : backNormalSprite,
                selectedSprite = backHoverSprite != null ? backHoverSprite : backNormalSprite,
                pressedSprite = backPressedSprite != null ? backPressedSprite : backNormalSprite
            };
            foreach (var label in backButton.GetComponentsInChildren<Text>()) label.enabled = false;
        } else {
            backButton.image.color = WoodButtonColor;
        }

        panel.SetActive(false);
    }

    private static Slider CreateVolumeRow(
        Transform parent,
        string title,
        Vector2 position,
        Sprite iconSprite,
        Sprite trackSprite,
        Sprite fillSprite,
        Sprite handleSprite,
        Sprite handlePressedSprite,
        float initialValue,
        UnityEngine.Events.UnityAction onIconClick,
        out Image iconImage,
        out Image fillImage,
        out Text valueLabel,
        UnityEngine.Events.UnityAction<float> onChanged) {
        var row = UiFactory.Rect(title + " Row", parent, new Vector2(.5f, .5f), position, new Vector2(720f, 76f));

        var iconRect = UiFactory.Rect(title + " Icon", row, new Vector2(.5f, .5f), new Vector2(-256f, 0f), new Vector2(68f, 68f));
        iconImage = iconRect.gameObject.AddComponent<Image>();
        if (iconSprite != null) {
            iconImage.sprite = iconSprite;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
        } else {
            iconImage.color = WoodButtonColor;
        }
        iconImage.raycastTarget = true;
        var iconButton = iconRect.gameObject.AddComponent<Button>();
        iconButton.targetGraphic = iconImage;
        iconButton.transition = Selectable.Transition.None;
        iconButton.onClick.AddListener(GameAudio.PlayButtonClick);
        iconButton.onClick.AddListener(onIconClick);

        var sliderRect = UiFactory.Rect(title + " Slider", row, new Vector2(.5f, .5f), new Vector2(22f, 0f), new Vector2(420f, 38f));
        var bg = UiFactory.Rect("Background", sliderRect, Vector2.zero, Vector2.zero, Vector2.zero);
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = bg.offsetMax = Vector2.zero;
        var bgImage = bg.gameObject.AddComponent<Image>();
        if (trackSprite != null) {
            bgImage.sprite = trackSprite;
            bgImage.color = Color.white;
        } else {
            bgImage.color = new Color(.18f, .24f, .32f, 1f);
        }

        var fillArea = UiFactory.Rect("Fill Area", sliderRect, Vector2.zero, Vector2.zero, Vector2.zero);
        fillArea.anchorMin = new Vector2(3f / 300f, 3f / 27f);
        fillArea.anchorMax = new Vector2(297f / 300f, 24f / 27f);
        fillArea.offsetMin = fillArea.offsetMax = Vector2.zero;

        var fill = UiFactory.Rect("Fill", fillArea, Vector2.zero, Vector2.zero, Vector2.zero);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        fillImage = fill.gameObject.AddComponent<Image>();
        if (fillSprite != null) {
            fillImage.sprite = fillSprite;
            fillImage.color = Color.white;
        } else {
            fillImage.color = new Color(1f, .84f, .12f, 1f);
        }

        var handleSlideArea = UiFactory.Rect("Handle Slide Area", sliderRect, Vector2.zero, Vector2.zero, Vector2.zero);
        handleSlideArea.anchorMin = new Vector2(12f / 300f, 0f);
        handleSlideArea.anchorMax = new Vector2(288f / 300f, 1f);
        handleSlideArea.offsetMin = handleSlideArea.offsetMax = Vector2.zero;

        var handle = UiFactory.Rect("Handle", handleSlideArea, new Vector2(.5f, .5f), Vector2.zero, new Vector2(58f, 58f));
        var handleImage = handle.gameObject.AddComponent<Image>();
        if (handleSprite != null) {
            handleImage.sprite = handleSprite;
            handleImage.preserveAspect = true;
        }
        handleImage.color = Color.white;

        var slider = sliderRect.gameObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        if (handleSprite != null && handlePressedSprite != null) {
            slider.transition = Selectable.Transition.SpriteSwap;
            slider.spriteState = new SpriteState {
                highlightedSprite = handleSprite,
                selectedSprite = handleSprite,
                pressedSprite = handlePressedSprite
            };
            var pressVisual = sliderRect.gameObject.AddComponent<SliderHandlePressVisual>();
            pressVisual.Initialize(handleImage, handleSprite, handlePressedSprite);
        }
        slider.SetValueWithoutNotify(initialValue);
        slider.onValueChanged.AddListener(onChanged);

        valueLabel = UiFactory.Label(row, "", new Vector2(.5f, .5f), new Vector2(288f, 0f), new Vector2(90f, 44f), 24);
        valueLabel.alignment = TextAnchor.MiddleLeft;
        valueLabel.color = HeaderTextColor;
        valueLabel.fontStyle = FontStyle.Bold;
        return slider;
    }

    private void ToggleBgmMute() {
        if (GameAudio.BgmVolume > MuteEpsilon) {
            lastUnmutedBgmVolume = GameAudio.BgmVolume;
            GameAudio.BgmVolume = 0f;
        } else {
            GameAudio.BgmVolume = lastUnmutedBgmVolume > MuteEpsilon ? lastUnmutedBgmVolume : 0.7f;
        }
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(GameAudio.BgmVolume);
        UpdateVolumeVisuals();
    }

    private void ToggleSfxMute() {
        if (GameAudio.SfxVolume > MuteEpsilon) {
            lastUnmutedSfxVolume = GameAudio.SfxVolume;
            GameAudio.SfxVolume = 0f;
        } else {
            GameAudio.SfxVolume = lastUnmutedSfxVolume > MuteEpsilon ? lastUnmutedSfxVolume : 1f;
        }
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameAudio.SfxVolume);
        UpdateVolumeVisuals();
    }

    private sealed class SliderHandlePressVisual : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler {
        private Image handleImage;
        private Sprite normalSprite;
        private Sprite pressedSprite;
        private bool isPointerDown;
        private bool isDragging;

        public void Initialize(Image targetImage, Sprite normal, Sprite pressed) {
            handleImage = targetImage;
            normalSprite = normal;
            pressedSprite = pressed;
        }

        public void OnPointerDown(PointerEventData eventData) {
            isPointerDown = true;
            RefreshSprite();
        }

        public void OnPointerUp(PointerEventData eventData) {
            isPointerDown = false;
            RefreshSprite();
        }

        public void OnBeginDrag(PointerEventData eventData) {
            isDragging = true;
            RefreshSprite();
        }

        public void OnEndDrag(PointerEventData eventData) {
            isDragging = false;
            RefreshSprite();
        }

        private void RefreshSprite() {
            if (handleImage == null) return;
            bool pressed = isPointerDown || isDragging;
            handleImage.sprite = pressed && pressedSprite != null ? pressedSprite : normalSprite;
            handleImage.overrideSprite = pressed ? pressedSprite : null;
        }
    }

    private void UpdateVolumeVisuals() {
        bool bgmMuted = GameAudio.BgmVolume <= MuteEpsilon;
        bool sfxMuted = GameAudio.SfxVolume <= MuteEpsilon;
        if (bgmValueLabel != null) bgmValueLabel.text = Mathf.RoundToInt(GameAudio.BgmVolume * 100f) + "%";
        if (sfxValueLabel != null) sfxValueLabel.text = Mathf.RoundToInt(GameAudio.SfxVolume * 100f) + "%";
        if (bgmFillImage != null) bgmFillImage.enabled = !bgmMuted;
        if (sfxFillImage != null) sfxFillImage.enabled = !sfxMuted;
        if (bgmIconImage != null) {
            var target = bgmMuted && musicMutedIconSprite != null ? musicMutedIconSprite : musicIconSprite;
            if (target != null) bgmIconImage.sprite = target;
        }
        if (sfxIconImage != null) {
            var target = sfxMuted && sfxMutedIconSprite != null ? sfxMutedIconSprite : sfxIconSprite;
            if (target != null) sfxIconImage.sprite = target;
        }
    }

    private void ToggleDevMode() { PlayerSession.DevMode = !PlayerSession.DevMode; RefreshDevLabel(); }
    private void RefreshDevLabel() { devLabel.text = "Developer Mode: " + (PlayerSession.DevMode ? "ON" : "OFF"); }
    public void Open(Action onReturn) {
        RefreshDevLabel();
        if (GameAudio.BgmVolume > MuteEpsilon) lastUnmutedBgmVolume = GameAudio.BgmVolume;
        if (GameAudio.SfxVolume > MuteEpsilon) lastUnmutedSfxVolume = GameAudio.SfxVolume;
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(GameAudio.BgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameAudio.SfxVolume);
        UpdateVolumeVisuals();
        returnAction = onReturn;
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        GameAudio.PlayHomeBgm();
    }
    public void Hide() { panel.SetActive(false); returnAction = null; }
    private void Close() { Action callback = returnAction; Hide(); callback?.Invoke(); }
}
}
