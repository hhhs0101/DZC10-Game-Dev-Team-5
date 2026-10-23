using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Defense {
public static class GameAudio {
    private static GameAudioCatalog catalog;
    private static AudioSource bgmSource;
    private static AudioSource sfxSource;

    private static GameAudioCatalog Catalog {
        get {
            if (catalog == null)
                catalog = Resources.Load<GameAudioCatalog>("GameAudioCatalog");
            return catalog;
        }
    }

    private static void EnsureSources() {
        if (!Application.isPlaying) return;
        if (bgmSource != null && sfxSource != null) return;
        var host = new GameObject("GameAudio");
        UnityEngine.Object.DontDestroyOnLoad(host);
        bgmSource = host.AddComponent<AudioSource>();
        bgmSource.playOnAwake = false;
        bgmSource.spatialBlend = 0f;
        bgmSource.loop = true;
        bgmSource.volume = 0.65f;

        sfxSource = host.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.loop = false;
        sfxSource.volume = 1f;
    }

    private static AudioClip Resolve(AudioClip clip, string assetPath) {
        if (clip != null) return clip;
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

    public static void PlayTowerPlaced(TurretDefinition tower) {
        if (tower == null || string.IsNullOrEmpty(tower.DisplayName)) return;
        string name = tower.DisplayName.Trim().ToLowerInvariant();
        if (name.Contains("ketchup") || name.Contains("mayonnaise") || name.Contains("mustard")) {
            PlaySfx(Resolve(Catalog != null ? Catalog.ketchupMayoMustard : null, "Assets/SFX/ketchup_mayo_mustard.wav"));
        } else if (name.Contains("olive oil") || name.Contains("soy sauce") || name.Contains("soya sauce")) {
            PlaySfx(Resolve(Catalog != null ? Catalog.oilSoya : null, "Assets/SFX/oil_soya.wav"));
        } else if (name.Contains("paprika")) {
            PlaySfx(Resolve(Catalog != null ? Catalog.paprika : null, "Assets/SFX/paprika.wav"));
        } else if (name.Contains("black pepper") || name == "pepper") {
            PlaySfx(Resolve(Catalog != null ? Catalog.pepper : null, "Assets/SFX/pepper.wav"));
        } else if (name.Contains("salt")) {
            PlaySfx(Resolve(Catalog != null ? Catalog.salt : null, "Assets/SFX/salt.wav"));
        }
    }

    private static void PlayBgm(AudioClip clip, bool loop) {
        if (!Application.isPlaying || clip == null) return;
        EnsureSources();
        if (bgmSource.clip == clip && bgmSource.isPlaying && bgmSource.loop == loop) return;
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    private static void PlaySfx(AudioClip clip) {
        if (!Application.isPlaying || clip == null) return;
        EnsureSources();
        sfxSource.PlayOneShot(clip);
    }
}
}

