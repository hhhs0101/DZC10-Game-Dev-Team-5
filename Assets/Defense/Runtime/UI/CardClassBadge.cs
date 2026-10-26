using UnityEngine;
using UnityEngine.UI;
namespace Defense {
public enum CardCombatClass { Basic, Splash, Pierce, Support, Burn, Slow, Skill }
// Shared presentation mapping, deliberately independent from card names and artwork.
public sealed class CardClassBadge : MonoBehaviour {
    private CardClassSymbol symbol;
    public static CardCombatClass ClassOf(CardDefinition card) {
        if(card is SkillCardDefinition) return CardCombatClass.Skill;
        if(card is TowerCardDefinition tower && tower.Tower != null) {
            switch(tower.Tower.TowerType) {
                case TowerType.Ketchup:return CardCombatClass.Splash;
                case TowerType.Mustard:return CardCombatClass.Pierce;
                case TowerType.Mayonnaise:return CardCombatClass.Support;
                case TowerType.HotSauce:return CardCombatClass.Burn;
                case TowerType.Oil:return CardCombatClass.Slow;
            }
        }
        return CardCombatClass.Basic;
    }
    public static Color Tint(CardCombatClass kind) {
        switch(kind) {
            case CardCombatClass.Splash:return new Color(1f,.5f,.4f);
            case CardCombatClass.Pierce:return new Color(1f,.84f,.35f);
            case CardCombatClass.Support:return new Color(.55f,.94f,.7f);
            case CardCombatClass.Burn:return new Color(1f,.62f,.3f);
            case CardCombatClass.Slow:return new Color(.5f,.84f,1f);
            case CardCombatClass.Skill:return new Color(.83f,.69f,1f);
            default:return new Color(.96f,.94f,.84f);
        }
    }
    public static CardClassBadge Create(RectTransform card) {
        float width=Mathf.Clamp(card.sizeDelta.x*.35f,30,72);
        var rect=UiFactory.Rect("Card Class",card,new Vector2(1,1),new Vector2(-width*.5f-4,-width*.5f-4),new Vector2(width,width));
        var bg=rect.gameObject.AddComponent<Image>();bg.color=new Color(.12f,.17f,.19f,.96f);bg.raycastTarget=false;
        var view=rect.gameObject.AddComponent<CardClassBadge>();
        var icon=UiFactory.Rect("Class Symbol",rect,new Vector2(.5f,.5f),Vector2.zero,new Vector2(width*.82f,width*.82f));
        icon.gameObject.AddComponent<CanvasRenderer>();
        view.symbol=icon.gameObject.AddComponent<CardClassSymbol>();view.symbol.raycastTarget=false;
        return view;
    }
    public void Bind(CardDefinition card) {
        gameObject.SetActive(card!=null);if(card==null)return;
        var kind=ClassOf(card);symbol.SetKind(kind);symbol.color=Tint(kind);
    }
}
}
