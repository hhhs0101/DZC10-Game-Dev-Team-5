using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Defense.Editor {
public static class TutorialAndCardClassValidation {
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    [MenuItem("Defense/Validate Tutorial And Card Classes")]
    public static void Run(){
        if(Application.isPlaying||TutorialUI.IsOpen)throw new InvalidOperationException("Close the guide and exit Play Mode first.");
        var root=new GameObject("Tutorial validation",typeof(RectTransform),typeof(Canvas));
        root.GetComponent<RectTransform>().sizeDelta=new Vector2(1280,720);
        bool seen=TutorialUI.HasSeen;
        try {
            var catalog=AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/Defense/Resources/GameCatalog.asset");
            var cardRect=UiFactory.Rect("Test Card",root.transform,Vector2.zero,Vector2.zero,new Vector2(100,156));
            // Reproduce the reported deck scroll-mask disable/enable lifecycle.
            var mask=UiFactory.Rect("Scroll mask",root.transform,Vector2.zero,Vector2.zero,new Vector2(300,300));
            mask.gameObject.AddComponent<RectMask2D>();cardRect.SetParent(mask,false);
            var badge=CardClassBadge.Create(cardRect);
            Check(badge.GetComponentInChildren<CardClassSymbol>().GetComponent<CanvasRenderer>()!=null,"Card icon has its renderer before clipping starts");
            Canvas.ForceUpdateCanvases();
            cardRect.gameObject.SetActive(false);cardRect.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            var standalone=UiFactory.Rect("Standalone symbol",root.transform,Vector2.zero,Vector2.zero,new Vector2(30,30));
            standalone.gameObject.AddComponent<CardClassSymbol>();
            Check(standalone.GetComponent<CanvasRenderer>()!=null,"Symbol declares its renderer dependency for every creation path");
            var classes=new System.Collections.Generic.HashSet<CardCombatClass>();
            foreach(var card in catalog.AvailableCards){
                badge.Bind(card);var kind=CardClassBadge.ClassOf(card);classes.Add(kind);
                Check(badge.GetComponentInChildren<Text>()==null && badge.GetComponentInChildren<CardClassSymbol>()!=null,"Class badge uses an icon without a text label");
                Check(badge.GetComponentsInChildren<Graphic>().All(g=>!g.raycastTarget),"Badge never intercepts drag/click input");
                if(card is SkillCardDefinition)Check(kind==CardCombatClass.Skill,"Skill card classification");
                if(card is TowerCardDefinition t&&t.Tower!=null){
                    switch(t.Tower.TowerType){
                        case TowerType.Ketchup:Check(kind==CardCombatClass.Splash,"Ketchup splash");break;
                        case TowerType.Mustard:Check(kind==CardCombatClass.Pierce,"Mustard piercing");break;
                        case TowerType.Mayonnaise:Check(kind==CardCombatClass.Support,"Mayonnaise support");break;
                        case TowerType.HotSauce:Check(kind==CardCombatClass.Burn,"Hot Sauce burn");break;
                        case TowerType.Oil:Check(kind==CardCombatClass.Slow,"Oil slow");break;
                        default:Check(kind==CardCombatClass.Basic,"Direct damage class");break;
                    }
                }
            }
            Check(classes.Count==7,"All seven classes represented");badge.Bind(null);Check(!badge.gameObject.activeSelf,"Empty slots hide badge");
            TutorialUI.Open(root.transform);TutorialUI.Open(root.transform);
            Check(root.GetComponentsInChildren<TutorialUI>().Length==1,"Duplicate guide suppressed");
            var tutorial=root.GetComponentInChildren<TutorialUI>();
            Check(tutorial.GetComponentsInChildren<CardClassSymbol>(true).All(icon=>icon.GetComponent<CanvasRenderer>()!=null),"Tutorial legend icons also have renderers");
            Check(TutorialUI.BlocksInput,"Guide blocks underlying shortcuts");
            var next=tutorial.GetComponentsInChildren<Button>().Single(b=>b.name=="Next");
            for(int i=0;i<3;i++)next.onClick.Invoke();
            Check(next.GetComponentInChildren<Text>().text=="Close","Last page offers completion");
            Check(tutorial.GetComponentsInChildren<Text>().Any(t=>t.text=="4 / 4"),"Guide has four pages");
            Check(tutorial.GetComponentsInChildren<Text>().Any(t=>t.text=="Towers"),"Final page describes towers");
            Check(tutorial.GetComponentsInChildren<CardClassSymbol>().Length==6,"Tower descriptions have matching class symbols");
            Check(TutorialUI.HasSeen==seen,"Viewing pages alone does not change completion state");
            var back=tutorial.GetComponentsInChildren<Button>().Single(b=>b.name=="Back");
            for(int i=0;i<3;i++)back.onClick.Invoke();Check(!back.interactable,"Back disabled on first page");
            Debug.Log("TUTORIAL AND CARD CLASSES PASS: mapping, icon-only badges, non-blocking badges, empty slots, modal uniqueness and navigation. Playtest first-run/replay and pause behavior separately.");
        }finally{UnityEngine.Object.DestroyImmediate(root);}
        Check(!TutorialUI.IsOpen,"Destroyed guide clears modal state");
    }
}
}
