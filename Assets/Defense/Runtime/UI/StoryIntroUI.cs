using System;
using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Pokémon-style dialogue between Mom and Jihoon, played once per launch when Start Game opens stage select.
// Portraits are loaded from Resources/Story/{Mom|Jihoon}_{Face}; placeholders are drawn if the art is missing.
public sealed class StoryIntroUI : MonoBehaviour {
    private enum Speaker { Mom, Jihoon }
    private readonly struct Line {
        public readonly Speaker Speaker; public readonly bool Thought; public readonly string MomFace, JihoonFace, Korean, English;
        public Line(Speaker speaker, bool thought, string momFace, string jihoonFace, string korean, string english) {
            Speaker = speaker; Thought = thought; MomFace = momFace; JihoonFace = jihoonFace; Korean = korean; English = english;
        }
    }
    private static readonly Line[] Script = {
        new Line(Speaker.Mom,false,"Regular","Regular","지훈아! 저녁 다 됐다~ 오늘은 채소 듬뿍 넣은 국이야!","Jihoon! Dinner's ready~ Tonight it's soup packed with vegetables!"),
        new Line(Speaker.Jihoon,false,"Regular","Regular","채... 채소?! 으악! 엄마, 나 배 안 고파...","Ve... vegetables?! Ugh! Mom, I'm not hungry..."),
        new Line(Speaker.Mom,false,"Angry","Regular","또 그런다! 넌 왜 이렇게 입이 짧니?","Not again! Why is your mouth so short?"),
        new Line(Speaker.Mom,false,"Angry","Regular","채소는 몸에 좋아. 오늘은 한 그릇 다 먹어야 해!","Vegetables are good for you. You're finishing the whole bowl today!"),
        new Line(Speaker.Jihoon,false,"Angry","Regular","...네에. 알겠어요.","...Okaaay. Fine."),
        new Line(Speaker.Jihoon,true,"Regular","Mischievous","(흐흐... 하지만 나한테는 비밀 작전이 있지!)","(Heh heh... but I've got a secret plan!)"),
        new Line(Speaker.Jihoon,true,"Regular","Mischievous","(채소가 냄비에 들어가기 전에, 내가 먼저 맛있게 양념해 버릴 거야!)","(Before the veggies go into the pot, I'll season them first so they taste good!)"),
        new Line(Speaker.Jihoon,true,"Regular","Happy","(소금, 간장, 매운 소스... 엄마는 절대 모를걸!)","(Salt, soy sauce, hot sauce... Mom will never know!)"),
    };
    private const float CharsPerSecond = 28f, FadeSeconds = .4f;
    private static readonly Color Ink = new Color(.16f,.12f,.1f), Dimmed = new Color(.45f,.45f,.5f,1f);
    private static bool played;

    private Action finished;
    private CanvasGroup group;
    private Image mom, jihoon, thoughtTint;
    private RectTransform momRect, jihoonRect;
    private Vector2 momBase, jihoonBase;
    private Text nameTag, korean, english, arrow;
    private RectTransform nameTagRect;
    private int index = -1;
    private float typed, fade, time;
    private bool closing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { played = false; }

    public static bool ShouldPlay => !played;

    public static void Play(Transform canvas, Sprite background, Action onFinished) {
        played = true;
        var root = UiFactory.Panel("Story Intro",canvas,Color.black);
        root.transform.SetAsLastSibling();
        root.AddComponent<StoryIntroUI>().Build(background,onFinished);
    }

    private void Build(Sprite background, Action onFinished) {
        finished = onFinished;
        group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;

        if (background != null) {
            var bg = UiFactory.Rect("Kitchen",transform,new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
            var bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.sprite = background; bgImage.preserveAspect = true; bgImage.raycastTarget = false;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectRatio = background.rect.width / background.rect.height;
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        }
        // Darken the kitchen so the portraits and dialogue stand out.
        UiFactory.Panel("Background Dim",transform,new Color(0,0,0,.5f)).GetComponent<Image>().raycastTarget = false;
        thoughtTint = UiFactory.Panel("Thought Tint",transform,new Color(.35f,.15f,.55f,.35f)).GetComponent<Image>();
        thoughtTint.raycastTarget = false; thoughtTint.enabled = false;

        // Mom on the left, Jihoon on the right; the busts tuck behind the top of the dialogue box.
        mom = Character("Mom",new Vector2(.03f,.2f),new Vector2(.4f,.97f),new Color(.93f,.55f,.62f),out momRect);
        jihoon = Character("Jihoon",new Vector2(.62f,.2f),new Vector2(.95f,.88f),new Color(.98f,.82f,.3f),out jihoonRect);
        // Mom's art looks to the left; mirror it so she faces Jihoon.
        if (mom.sprite != null) momRect.localScale = new Vector3(-1,1,1);
        momBase = momRect.anchoredPosition; jihoonBase = jihoonRect.anchoredPosition;

        var frame = UiFactory.Panel("Dialogue Box",transform,new Color(.13f,.18f,.34f)).GetComponent<RectTransform>();
        Stretch(frame,new Vector2(.03f,.02f),new Vector2(.97f,.27f));
        var inner = UiFactory.Panel("Inner",frame,new Color(.98f,.96f,.9f)).GetComponent<RectTransform>();
        inner.offsetMin = new Vector2(8,8); inner.offsetMax = new Vector2(-8,-8);
        var stripe = UiFactory.Panel("Stripe",frame,new Color(.78f,.22f,.2f)).GetComponent<RectTransform>();
        stripe.offsetMin = new Vector2(4,4); stripe.offsetMax = new Vector2(-4,-4); stripe.SetSiblingIndex(0);
        frame.GetComponent<Image>().raycastTarget = false;

        // Typed text keeps a fixed size; best-fit would make the letters shrink as each line grows.
        korean = Text(inner,new Vector2(.03f,.48f),new Vector2(.95f,.94f),28,TextAnchor.UpperLeft);
        english = Text(inner,new Vector2(.03f,.06f),new Vector2(.95f,.48f),23,TextAnchor.UpperLeft);
        korean.resizeTextForBestFit = english.resizeTextForBestFit = false;
        // Solid ink, not faded: semi-transparent small text renders soft and is hard to read.
        english.color = new Color(.27f,.2f,.36f);
        arrow = Text(inner,new Vector2(.95f,.04f),new Vector2(.995f,.3f),26,TextAnchor.MiddleCenter);
        arrow.text = "▼"; arrow.color = new Color(.78f,.22f,.2f);

        var tag = UiFactory.Panel("Name Tag",transform,new Color(.13f,.18f,.34f));
        nameTagRect = tag.GetComponent<RectTransform>();
        nameTag = Text(nameTagRect,Vector2.zero,Vector2.one,26,TextAnchor.MiddleCenter);
        nameTag.color = Color.white; nameTag.fontStyle = FontStyle.Bold;

        var skip = UiFactory.Button(transform,"Skip >>",new Vector2(-20,-20),Finish,new Vector2(1,1),new Vector2(150,44));
        ((RectTransform)skip.transform).pivot = new Vector2(1,1);
        skip.image.color = new Color(0,0,0,.45f);

        // Clicking anywhere else advances the dialogue.
        var advance = gameObject.AddComponent<Button>();
        advance.transition = Selectable.Transition.None;
        advance.onClick.AddListener(Advance);
        Next();
    }

    private Image Character(string who, Vector2 min, Vector2 max, Color placeholder, out RectTransform rect) {
        rect = UiFactory.Rect(who,transform,Vector2.zero,Vector2.zero,Vector2.zero);
        Stretch(rect,min,max);
        var image = rect.gameObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.sprite = UiFactory.LoadSprite("Story/" + who + "_Regular");
        if (image.sprite != null) { image.preserveAspect = true; return image; }
        // Placeholder figure: a coloured body and a head, so the scene reads correctly before art is added.
        image.color = Color.clear;
        var body = UiFactory.Panel("Body",rect,placeholder).GetComponent<RectTransform>();
        Stretch(body,new Vector2(.2f,0),new Vector2(.8f,.62f));
        var head = UiFactory.Panel("Head",rect,new Color(1f,.86f,.72f)).GetComponent<RectTransform>();
        Stretch(head,new Vector2(.28f,.64f),new Vector2(.72f,1f));
        var label = UiFactory.Label(body,who == "Mom" ? "엄마\nMom" : "지훈\nJihoon",new Vector2(.5f,.5f),Vector2.zero,new Vector2(200,90),26);
        label.color = Ink;
        return image;
    }

    private void Update() {
        time += Time.unscaledDeltaTime;
        fade = Mathf.MoveTowards(fade,closing ? 0 : 1,Time.unscaledDeltaTime / FadeSeconds);
        group.alpha = fade;
        if (closing) { if (fade <= 0) Complete(); return; }

        if (Input.GetKeyDown(KeyCode.Escape)) { Finish(); return; }
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Advance();
        if (closing) return;

        var line = Script[index];
        int total = line.Korean.Length + line.English.Length;
        typed = Mathf.Min(total,typed + Time.unscaledDeltaTime * CharsPerSecond);
        int shown = Mathf.FloorToInt(typed);
        korean.text = line.Korean.Substring(0,Mathf.Min(shown,line.Korean.Length));
        english.text = line.English.Substring(0,Mathf.Clamp(shown - line.Korean.Length,0,line.English.Length));
        arrow.enabled = shown >= total && Mathf.Repeat(time,.8f) < .5f;

        // The speaking character bobs gently, like a talking sprite.
        float bob = Mathf.Abs(Mathf.Sin(time * 9f)) * 8f;
        momRect.anchoredPosition = momBase + (line.Speaker == Speaker.Mom && shown < total ? new Vector2(0,bob) : Vector2.zero);
        jihoonRect.anchoredPosition = jihoonBase + (line.Speaker == Speaker.Jihoon && shown < total ? new Vector2(0,bob) : Vector2.zero);
    }

    private void Advance() {
        if (closing || index < 0) return;
        var line = Script[index];
        int total = line.Korean.Length + line.English.Length;
        if (typed < total) { typed = total; return; }
        Next();
    }

    private void Next() {
        index++;
        if (index >= Script.Length) { Finish(); return; }
        typed = 0;
        var line = Script[index];
        bool momTalks = line.Speaker == Speaker.Mom;
        SetFace(mom,"Mom",line.MomFace);
        SetFace(jihoon,"Jihoon",line.JihoonFace);
        Tint(mom,momTalks);
        Tint(jihoon,!momTalks);
        thoughtTint.enabled = line.Thought;
        nameTag.text = momTalks ? "엄마 - Mom" : "지훈 - Jihoon";
        // The name tag sits on the dialogue box edge, on the side of whoever is speaking.
        Stretch(nameTagRect,momTalks ? new Vector2(.05f,.27f) : new Vector2(.73f,.27f),momTalks ? new Vector2(.27f,.33f) : new Vector2(.95f,.33f));
    }

    // Swaps the portrait to match the line's tone; keeps the current face if that expression is missing.
    private static void SetFace(Image character, string who, string face) {
        if (character.sprite == null) return;
        var sprite = UiFactory.LoadSprite("Story/" + who + "_" + face);
        if (sprite != null) character.sprite = sprite;
    }
    private static void Tint(Image character, bool speaking) {
        var color = speaking ? Color.white : Dimmed;
        if (character.sprite != null) character.color = color;
        foreach (var part in character.GetComponentsInChildren<Image>())
            if (part != character) part.canvasRenderer.SetColor(color);
    }

    private void Finish() { closing = true; }

    private void Complete() {
        if (finished == null) return;
        var callback = finished; finished = null;
        Destroy(gameObject);
        callback();
    }

    private static Text Text(Transform parent, Vector2 min, Vector2 max, int size, TextAnchor anchor) {
        var text = UiFactory.Label(parent,"",Vector2.zero,Vector2.zero,Vector2.zero,size);
        Stretch(text.rectTransform,min,max);
        text.color = Ink; text.alignment = anchor;
        text.resizeTextForBestFit = true; text.resizeTextMinSize = 12; text.resizeTextMaxSize = size;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max) {
        rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
}
