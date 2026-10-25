using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
namespace Defense {
// Plays Resources/Video/Intro full screen once per launch; any key or click skips it.
public sealed class IntroVideoUI : MonoBehaviour {
    private const string ClipPath = "Video/Intro";
    private static bool played;
    private VideoPlayer player;
    private RenderTexture texture;
    private Action finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { played = false; }

    public static bool ShouldPlay => !played && Resources.Load<VideoClip>(ClipPath) != null;

    public static void Play(Transform canvas, Action onFinished) {
        played = true;
        var clip = Resources.Load<VideoClip>(ClipPath);
        if (clip == null) { onFinished?.Invoke(); return; }
        var rect = UiFactory.Rect("Intro Video",canvas,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetAsLastSibling();
        var backdrop = rect.gameObject.AddComponent<Image>();
        backdrop.color = Color.black;
        var view = UiFactory.Rect("Frame",rect,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
        view.anchorMin = Vector2.zero; view.anchorMax = Vector2.one; view.offsetMin = view.offsetMax = Vector2.zero;
        var image = view.gameObject.AddComponent<RawImage>();
        var fit = view.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = clip.height > 0 ? (float)clip.width / clip.height : 16f / 9f;
        rect.gameObject.AddComponent<IntroVideoUI>().Begin(clip,image,onFinished);
    }

    private void Begin(VideoClip clip, RawImage image, Action onFinished) {
        finished = onFinished;
        texture = new RenderTexture((int)Mathf.Max(clip.width,1),(int)Mathf.Max(clip.height,1),0);
        image.texture = texture;
        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = false;
        player.clip = clip;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = texture;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        for (ushort i = 0; i < clip.audioTrackCount; i++) player.SetDirectAudioVolume(i,GameAudio.BgmVolume);
        player.loopPointReached += _ => Finish();
        player.errorReceived += (_, message) => { Debug.LogWarning($"Intro video failed: {message}"); Finish(); };
        player.Play();
    }

    private void Update() {
        if (Input.anyKeyDown) Finish();
    }

    private void Finish() {
        if (finished == null) return;
        var callback = finished; finished = null;
        Destroy(gameObject);
        callback();
    }

    private void OnDestroy() {
        if (texture != null) { texture.Release(); Destroy(texture); }
    }
}
}
