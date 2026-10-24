using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Defense {
public enum SoundCategory {
    Bgm,
    Sfx
}

public static class GameAudio {
    public const float TowerSfxDuration = 1f;
    private const string BgmVolumePrefKey = "Defense.Audio.BgmVolume";
    private const string SfxVolumePrefKey = "Defense.Audio.SfxVolume";

    private static GameAudioCatalog catalog;
    private static AudioSource bgmSource;
    private static AudioSource sfxSource;
    private static AudioSource towerSfxSource;
    private static AudioSource enemyDeathSfxSource;
    private static AudioSource enemyReachSfxSource;
    private static float bgmVolume = -1f;
    private static float sfxVolume = -1f;

    public static event Action<SoundCategory, float> VolumeChanged;

    private static GameAudioCatalog Catalog {
        get {
            if (catalog == null)
                catalog = Resources.Load<GameAudioCatalog>("GameAudioCatalog");
            return catalog;
        }
    }

    public static float BgmVolume {
        get {
            if (bgmVolume < 0f)
                bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumePrefKey, 0.7f));
            return bgmVolume;
        }
        set => SetVolume(SoundCategory.Bgm, value);
    }

    public static float SfxVolume {
        get {
            if (sfxVolume < 0f)
                sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumePrefKey, 1f));
            return sfxVolume;
        }
        set => SetVolume(SoundCategory.Sfx, value);
    }

    public static float GetVolume(SoundCategory category) =>
        category == SoundCategory.Bgm ? BgmVolume : SfxVolume;

    public static void SetVolume(SoundCategory category, float value) {
        float clamped = Mathf.Clamp01(value);
        if (category == SoundCategory.Bgm) {
            bgmVolume = clamped;
            PlayerPrefs.SetFloat(BgmVolumePrefKey, clamped);
            if (bgmSource != null) bgmSource.volume = clamped;
        } else {
            sfxVolume = clamped;
            PlayerPrefs.SetFloat(SfxVolumePrefKey, clamped);
            if (sfxSource != null) sfxSource.volume = clamped;
            if (towerSfxSource != null) towerSfxSource.volume = clamped;
            if (enemyDeathSfxSource != null) enemyDeathSfxSource.volume = clamped;
            if (enemyReachSfxSource != null) enemyReachSfxSource.volume = clamped;
        }
        VolumeChanged?.Invoke(category, clamped);
    }

    private static void EnsureSources() {
        if (!Application.isPlaying) return;
        EnsureAudioListener();
        if (bgmSource != null && sfxSource != null && towerSfxSource != null && enemyDeathSfxSource != null && enemyReachSfxSource != null) return;
        var host = new GameObject("GameAudio");
        UnityEngine.Object.DontDestroyOnLoad(host);

        var bgmGroup = new GameObject("BGM Category");
        bgmGroup.transform.SetParent(host.transform, false);
        bgmSource = bgmGroup.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.loop = true;
        bgmSource.volume = BgmVolume;

        var sfxGroup = new GameObject("SFX Category");
        sfxGroup.transform.SetParent(host.transform, false);
        sfxSource = sfxGroup.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.loop = false;
        sfxSource.volume = SfxVolume;

        towerSfxSource = sfxGroup.AddComponent<AudioSource>();
        towerSfxSource.playOnAwake = false;
        towerSfxSource.spatialBlend = 0f;
        towerSfxSource.loop = false;
        towerSfxSource.volume = SfxVolume;

        enemyDeathSfxSource = sfxGroup.AddComponent<AudioSource>();
        enemyDeathSfxSource.playOnAwake = false;
        enemyDeathSfxSource.spatialBlend = 0f;
        enemyDeathSfxSource.loop = false;
        enemyDeathSfxSource.volume = SfxVolume;

        enemyReachSfxSource = sfxGroup.AddComponent<AudioSource>();
        enemyReachSfxSource.playOnAwake = false;
        enemyReachSfxSource.spatialBlend = 0f;
        enemyReachSfxSource.loop = false;
        enemyReachSfxSource.volume = SfxVolume;
    }

    private static void EnsureAudioListener() {
        if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() != null) return;
        var cam = Camera.main;
        if (cam != null) cam.gameObject.AddComponent<AudioListener>();
    }

    private static AudioClip Resolve(AudioClip clip, string assetPath, string resourcePath = null) {
        if (clip != null) return clip;
        if (!string.IsNullOrEmpty(resourcePath)) {
            var resClip = Resources.Load<AudioClip>(resourcePath);
            if (resClip != null) return resClip;
        }
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
#else
        return null;
#endif
    }

    public static void PlayIntroBgm() => PlayBgm(Resolve(Catalog != null ? Catalog.introTheme : null, "Assets/BGM/intro_theme.wav"), true);
    public static void PlayHomeBgm() => PlayBgm(Resolve(Catalog != null ? Catalog.homeBgm : null, "Assets/BGM/home_bgm.wav"), true);
    public static void PlayStageBgm() => PlayBgm(Resolve(Catalog != null ? Catalog.stageBgm : null, "Assets/BGM/stage_bgm.wav"), true);
    public static void PlayGameOverBgm() => PlayBgm(Resolve(Catalog != null ? Catalog.gameOverBgm : null, "Assets/BGM/GameOver.wav"), false);
    public static void PlayVictoryBgm() => PlayBgm(Resolve(Catalog != null ? Catalog.victoryBgm : null, "Assets/BGM/victory.wav"), false);

    public static void PlayButtonClick() => PlaySfx(Resolve(Catalog != null ? Catalog.buttonClick : null, "Assets/SFX/button_click.wav"));
    public static void PlayCardUse() => PlaySfx(Resolve(Catalog != null ? Catalog.cardUse : null, "Assets/SFX/card_use.wav"));
    public static void PlayEnemyAliveReaching() =>
        PlaySfx(Resolve(Catalog != null ? Catalog.enemyAliveReaching : null, "Assets/SFX/enemy_reach_alive.wav", "SFX/enemy_reach_alive"));
    public static void PlayEnemyDeadReaching() =>
        PlayCappedSfx(enemyReachSfxSource, Resolve(Catalog != null ? Catalog.enemyDeadReaching : null, "Assets/SFX/enemy_dead_reaching.mp3", "SFX/enemy_dead_reaching"), TowerSfxDuration, ref enemyReachSfxSource);
    public static void PlayEnemyNoHp() =>
        PlayCappedSfx(enemyDeathSfxSource, Resolve(Catalog != null ? Catalog.enemyNoHp : null, "Assets/SFX/enemy_no_hp.mp3", "SFX/enemy_no_hp"), TowerSfxDuration, ref enemyDeathSfxSource);

    public static void PlayTowerPlaced(TurretDefinition tower) {
        if (tower == null || string.IsNullOrEmpty(tower.DisplayName)) return;
        string name = tower.DisplayName.Trim().ToLowerInvariant();
        if (name.Contains("ketchup") || name.Contains("mayonnaise") || name.Contains("mustard")) {
            PlayTowerSfx(Resolve(Catalog != null ? Catalog.ketchupMayoMustard : null, "Assets/SFX/ketchup_mayo_mustard.wav"));
        } else if (name.Contains("olive oil") || name.Contains("soy sauce") || name.Contains("soya sauce")) {
            PlayTowerSfx(Resolve(Catalog != null ? Catalog.oilSoya : null, "Assets/SFX/oil_soya.wav"));
        } else if (name.Contains("paprika")) {
            PlayTowerSfx(Resolve(Catalog != null ? Catalog.paprika : null, "Assets/SFX/paprika.wav"));
        } else if (name.Contains("black pepper") || name == "pepper") {
            PlayTowerSfx(Resolve(Catalog != null ? Catalog.pepper : null, "Assets/SFX/pepper.wav"));
        } else if (name.Contains("salt")) {
            PlayTowerSfx(Resolve(Catalog != null ? Catalog.salt : null, "Assets/SFX/salt.wav"));
        }
    }

    private static void PlayBgm(AudioClip clip, bool loop) {
        if (!Application.isPlaying || clip == null) return;
        EnsureSources();
        bgmSource.volume = BgmVolume;
        if (bgmSource.clip == clip && bgmSource.isPlaying && bgmSource.loop == loop) return;
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    private static void PlaySfx(AudioClip clip) {
        if (!Application.isPlaying || clip == null) return;
        EnsureSources();
        sfxSource.volume = SfxVolume;
        sfxSource.PlayOneShot(clip, SfxVolume);
    }

    private static void PlayTowerSfx(AudioClip clip) =>
        PlayCappedSfx(towerSfxSource, clip, TowerSfxDuration, ref towerSfxSource);

    private static void PlayCappedSfx(AudioSource _, AudioClip clip, float maxDuration, ref AudioSource targetSource) {
        if (!Application.isPlaying || clip == null) return;
        EnsureSources();
        if (targetSource == null) return;
        targetSource.Stop();
        targetSource.clip = clip;
        targetSource.loop = false;
        targetSource.volume = SfxVolume;
        targetSource.time = 0f;
        double startDsp = AudioSettings.dspTime;
        double playDuration = clip.length > 0f ? Math.Min(clip.length, maxDuration) : maxDuration;
        targetSource.PlayScheduled(startDsp);
        targetSource.SetScheduledEndTime(startDsp + playDuration);
    }
}
}
