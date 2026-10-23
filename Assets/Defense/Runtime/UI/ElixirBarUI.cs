using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace Defense {
// One reusable block per integer point. Presentation observes the authoritative wallet.
public sealed class ElixirBarUI : MonoBehaviour {
    private ElixirSystem elixir;
    private readonly GameObject[] segments = new GameObject[ElixirSystem.Maximum];
    private Text energyNumber;
    public int DisplayedSegments { get; private set; }
    public float DisplayedFill => DisplayedSegments / (float)ElixirSystem.Maximum;
    public Text EnergyNumber => energyNumber;

    public void Initialize(Transform parent, ElixirSystem system) {
        elixir = system;
        var frameSprite = Resources.Load<Sprite>("UI/EnergyBarFrame");
        var segmentSprite = Resources.Load<Sprite>("UI/EnergySegment");
        if (frameSprite == null || segmentSprite == null) throw new System.InvalidOperationException("Segmented Energy UI sprites are missing from Resources/UI.");
        const float width = 88;
        float height = width * frameSprite.rect.height / frameSprite.rect.width;
        var background = UiFactory.Rect("Energy Bar",parent,new Vector2(1,.5f),new Vector2(-168,-15),new Vector2(width,height));
        var image = background.gameObject.AddComponent<Image>();
        image.sprite = frameSprite; image.preserveAspect = true; image.raycastTarget = false;
        // Revised source-frame crop is (222,97,294,1342); the dark interior is
        // (314,222)-(424,1195), measured from the original image's top-left.
        var interior = UiFactory.Rect("Energy Segments",background,Vector2.zero,Vector2.zero,Vector2.zero);
        interior.anchorMin = new Vector2(92f/294,244f/1342);
        interior.anchorMax = new Vector2(202f/294,1217f/1342);
        interior.offsetMin = interior.offsetMax = Vector2.zero;
        for (int i=0;i<segments.Length;i++) {
            var rect = UiFactory.Rect("Energy Segment "+(i+1),interior,Vector2.zero,Vector2.zero,Vector2.zero);
            rect.anchorMin = new Vector2(0,i/(float)segments.Length);
            rect.anchorMax = new Vector2(1,(i+1)/(float)segments.Length);
            rect.offsetMin = new Vector2(0,.5f); rect.offsetMax = new Vector2(0,-.5f);
            var block = rect.gameObject.AddComponent<Image>();
            block.sprite = segmentSprite; block.preserveAspect = true; block.raycastTarget = false;
            segments[i] = rect.gameObject;
        }
        // A final sibling draws over every segment, including a full bar.
        var boltSprite = Resources.Load<Sprite>("UI/EnergyBarBolt") ?? Resources.Load<Sprite>("UI/EnergyIcon");
        if (boltSprite != null) {
            const float boltWidth = 60;
            float boltHeight = boltWidth * boltSprite.rect.height / boltSprite.rect.width;
            var bolt = UiFactory.Rect("Energy Bolt Overlay",background,new Vector2(.5f,1),new Vector2(0,-12),
                new Vector2(boltWidth,boltHeight));
            var boltImage = bolt.gameObject.AddComponent<Image>();
            boltImage.sprite = boltSprite; boltImage.preserveAspect = true; boltImage.raycastTarget = false;
            bolt.SetAsLastSibling();

            energyNumber = UiFactory.Label(bolt,"",new Vector2(.43f,.73f),Vector2.zero,
                new Vector2(boltWidth*.82f,boltHeight*.42f),Mathf.RoundToInt(boltWidth*.62f));
            energyNumber.name = "Energy Number";
            energyNumber.color = Color.white;
            energyNumber.fontStyle = FontStyle.Bold;
            energyNumber.resizeTextForBestFit = true;
            energyNumber.resizeTextMinSize = 12;
            energyNumber.resizeTextMaxSize = Mathf.RoundToInt(boltWidth*.62f);
            energyNumber.horizontalOverflow = HorizontalWrapMode.Overflow;
            energyNumber.verticalOverflow = VerticalWrapMode.Overflow;
            var outline = energyNumber.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f,-1.5f);
            outline.useGraphicAlpha = true;
        }
        elixir.Changed += Refresh; Refresh(elixir.CurrentEnergy);
    }
    private void Refresh(int amount) {
        DisplayedSegments = Mathf.Clamp(amount,0,ElixirSystem.Maximum);
        for (int i=0;i<segments.Length;i++) segments[i].SetActive(i<DisplayedSegments);
        if (energyNumber != null) energyNumber.text = DisplayedSegments.ToString(CultureInfo.InvariantCulture);
    }
    // Compatibility hook for existing validation callers; no fractional interpolation.
    public void Advance(float deltaTime) { if (elixir != null) Refresh(elixir.CurrentEnergy); }
    private void OnDestroy() { if (elixir != null) elixir.Changed -= Refresh; }
}
}
