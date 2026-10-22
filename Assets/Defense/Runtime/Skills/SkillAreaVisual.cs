using UnityEngine;
using UnityEngine.SceneManagement;
namespace Defense {
// One presentation owner. Skills use a calibrated Sprite; Tower inspection keeps its circular line.
[RequireComponent(typeof(LineRenderer))]
public sealed class SkillAreaVisual : MonoBehaviour {
    private LineRenderer line;
    private Material material;
    private PersistentSlowArea area;
    private SpriteRenderer marker;
    public SpriteRenderer MarkerRenderer => marker;
    public EllipseFootprint Footprint { get; private set; }
    public bool IsVisible => (marker!=null&&marker.enabled)||(line!=null&&line.enabled);
    private void Awake() {line=GetComponent<LineRenderer>();Hide();}
    public void Observe(PersistentSlowArea runtime) {area=runtime;area.Changed+=Refresh;Refresh();}
    private void Refresh() {
        if(area!=null)ShowSkill(area.transform.position,area.Definition,new Color(1,1,1,area.VisualOpacity));
    }
    public void ShowSkill(Vector3 center,SkillDefinition definition,Color color) {
        if(!isActiveAndEnabled || color.a<=0) {Hide();return;}
        var profile=definition.RangeProfile;
        Footprint=definition.CreateFootprint(center);
        if(line==null)line=GetComponent<LineRenderer>();
        line.enabled=false;
        if(marker==null) {
            // Independent world transform avoids inherited scale/rotation/shear changing only the artwork.
            marker=new GameObject("Skill Glow Marker").AddComponent<SpriteRenderer>();
            if(gameObject.scene.IsValid()&&marker.gameObject.scene!=gameObject.scene)SceneManager.MoveGameObjectToScene(marker.gameObject,gameObject.scene);
        }
        var scale=profile.VisualScale(Footprint);var offset=profile.VisualOffset(Footprint);
        marker.transform.SetPositionAndRotation(center-new Vector3(offset.x,offset.y,0),Quaternion.identity);
        marker.transform.localScale=new Vector3(scale.x,scale.y,1);
        marker.sprite=profile.MarkerSprite;marker.color=color;marker.sortingOrder=30;
        marker.enabled=isActiveAndEnabled&&color.a>0;
    }
    // Retained circle API for Tower attack-range inspection; Skill code never uses this path.
    public void Show(Vector3 center,float radius,Color color,WaypointPlane plane=WaypointPlane.XZ) {
        if(marker!=null)marker.enabled=false;
        if(line==null)line=GetComponent<LineRenderer>();
        if(material==null) {
            line.useWorldSpace=true;line.loop=true;line.positionCount=96;line.widthMultiplier=.045f;
            material=new Material(Shader.Find("Sprites/Default"));line.sharedMaterial=material;
        }
        line.enabled=isActiveAndEnabled&&color.a>0;line.startColor=line.endColor=color;
        line.sortingOrder=plane==WaypointPlane.XY?30:0;
        for(int i=0;i<line.positionCount;i++) {
            float angle=i*Mathf.PI*2/line.positionCount;
            line.SetPosition(i,center+PlanarSpace.World(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius,plane==WaypointPlane.XY?0:.06f,plane));
        }
    }
    public void Hide() {if(line!=null)line.enabled=false;if(marker!=null)marker.enabled=false;}
    private void OnEnable()=>Refresh();
    private void OnDisable()=>Hide();
    private void OnDestroy() {
        Hide();if(area!=null)area.Changed-=Refresh;
        if(marker!=null)DestroyOwned(marker.gameObject);
        if(material!=null)DestroyOwned(material);
    }
    private static void DestroyOwned(Object value) {if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
}
}
