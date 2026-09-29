using UnityEngine;
using UnityEngine.UI;
namespace Defense {
[RequireComponent(typeof(Text))]
public sealed class FadingMessageUI : MonoBehaviour {
    private Text label;
    private float elapsed, hold;
    public const float FadeDuration = .3f;
    public void Show(string message, float holdSeconds) {
        label = GetComponent<Text>(); label.text = message; hold = holdSeconds; elapsed = 0;
        label.color = new Color(label.color.r,label.color.g,label.color.b,1);
    }
    private void Update() => Advance(Time.unscaledDeltaTime);
    public void Advance(float deltaTime) {
        if (label == null || label.text.Length == 0) return;
        elapsed += Mathf.Max(0,deltaTime);
        Color color = label.color; color.a = 1-Mathf.Clamp01((elapsed-hold)/FadeDuration); label.color = color;
        if (elapsed >= hold+FadeDuration) label.text = "";
    }
}
}
