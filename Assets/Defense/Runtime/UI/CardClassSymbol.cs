using UnityEngine;
using UnityEngine.UI;
namespace Defense {
// Vector UI icons: crisp at every card size, no font glyph or texture dependency.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class CardClassSymbol : MaskableGraphic {
    private CardCombatClass kind;
    public void SetKind(CardCombatClass value) {kind=value;SetVerticesDirty();}
    protected override void OnPopulateMesh(VertexHelper vh) {
        vh.Clear();
        switch(kind) {
            case CardCombatClass.Basic:
                // Symmetric crosshair with a solid centre stays readable on small cards.
                Ring(vh,.5f,.5f,.29f);
                Line(vh,.5f,.04f,.5f,.29f,.085f);Line(vh,.5f,.71f,.5f,.96f,.085f);
                Line(vh,.04f,.5f,.29f,.5f,.085f);Line(vh,.71f,.5f,.96f,.5f,.085f);
                Quad(vh,new Vector2(.44f,.44f),new Vector2(.44f,.56f),new Vector2(.56f,.56f),new Vector2(.56f,.44f));break;
            case CardCombatClass.Splash:
                Ring(vh,.5f,.5f,.14f);
                for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Line(vh,.5f+Mathf.Cos(a)*.23f,.5f+Mathf.Sin(a)*.23f,.5f+Mathf.Cos(a)*.42f,.5f+Mathf.Sin(a)*.42f);}break;
            case CardCombatClass.Pierce:
                // An arrow breaks through a shield, with a clear gap around its shaft.
                Line(vh,.19f,.8f,.55f,.8f,.075f);Line(vh,.19f,.8f,.22f,.42f,.075f);
                Line(vh,.22f,.42f,.31f,.31f,.075f);
                Line(vh,.43f,.2f,.5f,.12f,.075f);Line(vh,.5f,.12f,.76f,.42f,.075f);
                Line(vh,.76f,.42f,.79f,.58f,.075f);
                Line(vh,.12f,.16f,.72f,.76f,.11f);
                Polygon(vh,new[]{new Vector2(.91f,.94f),new Vector2(.85f,.56f),new Vector2(.54f,.87f)},new Vector2(.77f,.79f));break;
            case CardCombatClass.Support:
                Line(vh,.5f,.14f,.5f,.86f,.19f);Line(vh,.14f,.5f,.86f,.5f,.19f);break;
            case CardCombatClass.Burn:
                Polygon(vh,new[]{new Vector2(.5f,.94f),new Vector2(.73f,.65f),new Vector2(.7f,.48f),new Vector2(.85f,.57f),new Vector2(.84f,.28f),new Vector2(.65f,.1f),new Vector2(.38f,.1f),new Vector2(.18f,.31f),new Vector2(.24f,.6f),new Vector2(.38f,.45f)},new Vector2(.51f,.4f));break;
            case CardCombatClass.Slow:
                Line(vh,.2f,.85f,.8f,.85f);Line(vh,.2f,.15f,.8f,.15f);Line(vh,.25f,.8f,.75f,.2f);Line(vh,.75f,.8f,.25f,.2f);break;
            case CardCombatClass.Skill:
                var points=new Vector2[8];for(int i=0;i<8;i++){float a=i*Mathf.PI/4;float radius=i%2==0?.43f:.13f;points[i]=new Vector2(.5f+Mathf.Cos(a)*radius,.5f+Mathf.Sin(a)*radius);}Polygon(vh,points,new Vector2(.5f,.5f));break;
        }
    }
    private Vector3 Point(Vector2 p) {var r=GetPixelAdjustedRect();return new Vector3(r.x+p.x*r.width,r.y+p.y*r.height,0);}
    private void Quad(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Vector2 d) {int i=vh.currentVertCount;vh.AddVert(Point(a),color,Vector2.zero);vh.AddVert(Point(b),color,Vector2.zero);vh.AddVert(Point(c),color,Vector2.zero);vh.AddVert(Point(d),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);}
    private void Line(VertexHelper vh,float x,float y,float xx,float yy,float width=.065f) {var a=new Vector2(x,y);var b=new Vector2(xx,yy);var delta=(b-a).normalized;var normal=new Vector2(-delta.y,delta.x)*width*.5f;Quad(vh,a-normal,a+normal,b+normal,b-normal);}
    private void Ring(VertexHelper vh,float x,float y,float radius) {for(int i=0;i<32;i++){float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;Line(vh,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius,.055f);}}
    private void Polygon(VertexHelper vh,Vector2[] points,Vector2 center) {for(int j=0;j<points.Length;j++){int i=vh.currentVertCount;vh.AddVert(Point(center),color,Vector2.zero);vh.AddVert(Point(points[j]),color,Vector2.zero);vh.AddVert(Point(points[(j+1)%points.Length]),color,Vector2.zero);vh.AddTriangle(i,i+1,i+2);}}
}
}
