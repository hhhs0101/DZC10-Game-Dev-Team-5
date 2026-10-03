using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace Defense.Editor {
public static class PlacementCameraValidation {
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    public static void Run(Action<bool,string> check) {
        var camera=Camera.main; var surface=Find<TabletopSurface>();
        float angle=Mathf.Asin(-camera.transform.forward.y)*Mathf.Rad2Deg;
        check(Mathf.Abs(angle-40)<.01f && Mathf.Abs(camera.transform.forward.x)<.0001f && Vector3.Dot(camera.transform.right,Vector3.right)>.999f,"Camera faces front with 40-degree downward view and zero horizontal skew");
        var bounds=surface.GetComponent<Collider>().bounds;
        var left=camera.WorldToViewportPoint(new Vector3(bounds.min.x,bounds.max.y,bounds.min.z));
        var right=camera.WorldToViewportPoint(new Vector3(bounds.max.x,bounds.max.y,bounds.min.z));
        check(Mathf.Abs(left.y-right.y)<.0001f,"Projected front table edge is horizontal");
        bool visible=true;
        foreach(float x in new[]{bounds.min.x,bounds.max.x}) foreach(float z in new[]{bounds.min.z,bounds.max.z}) {
            var view=camera.WorldToViewportPoint(new Vector3(x,bounds.max.y,z));
            visible &= view.z>0 && view.x>.02f && view.x<.98f && view.y>.18f && view.y<.95f;
        }
        check(visible,"All tabletop corners remain visible above bottom Hand area");
        var hand=Find<GameplayHand>();var placement=Find<TurretPlacementController>();var wallet=Find<ResourceWallet>();var elixir=Find<ElixirSystem>();var flow=Find<GameFlow>();
        int savedGold=wallet.Balance;
        var slots=UnityEngine.Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None).OrderBy(s=>s.name).ToArray();
        var slot=slots.Last();var originalPoint=slot.PlacementPoint.localPosition;
        hand.Select(0); var selected=hand.SelectedCard; var before=hand.Cycle.Hand.ToArray();var queue=hand.Cycle.UpcomingQueue.ToArray();
        string message=null; placement.Message += value=>message=value;
        Physics.SyncTransforms();
        foreach(var target in slots) {
            placement.ProcessPointer(camera.WorldToScreenPoint(target.GetComponent<Collider>().bounds.center),false);
            check(target.IsHovered && !target.IsOccupied && hand.SelectedCard==selected,"Screen ray highlights configured slot without consuming selection: "+target.name);
        }
        Vector2 point=camera.WorldToScreenPoint(slot.GetComponent<Collider>().bounds.center);
        // Move an already rendered HUD button over the slot to test UI blocking in this same frame.
        var canvas=GameObject.Find("Canvas").GetComponent<RectTransform>();
        var rect=GameObject.Find("Hand Card 1").GetComponent<RectTransform>();
        var savedPosition=rect.position;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas,point,null,out var overlayPosition);
        rect.position=overlayPosition;Canvas.ForceUpdateCanvases();
        check(GameplayPointer.IsOverUI(point),"Existing Hand UI covers the slot screen coordinate for the blocking test");
        check(!placement.ProcessPointer(point,true) && hand.SelectedCard==selected && !slot.IsOccupied && !slot.IsHovered,"UI raycast consumes world click without losing selected card");
        rect.position=savedPosition;Canvas.ForceUpdateCanvases();
        float energy=elixir.Current;
        check(!placement.ProcessPointer(camera.WorldToScreenPoint(new Vector3(9,0,-4)),true) && message=="You cannot place a turret here." && hand.SelectedCard==selected,"Clicking tabletop outside slots preserves selected card and reports invalid location");
        wallet.Initialize(0);
        check(!placement.ProcessPointer(point,true) && message=="Not enough resources." && !slot.IsOccupied && elixir.Current==energy && hand.Cycle.Hand.SequenceEqual(before) && hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Screen-click failure spends no Elixir and preserves the full card cycle");
        wallet.Initialize(1000); flow.TogglePause();
        check(!placement.ProcessPointer(point,true) && !slot.IsOccupied && hand.SelectedCard==selected,"Paused screen input cannot place a tower");flow.TogglePause();
        // Verify child colliders resolve to the owning slot and an enemy collider cannot block the slot mask.
        var rootCollider=slot.GetComponent<BoxCollider>();rootCollider.enabled=false;
        var colliderChild=new GameObject("Child collider test");colliderChild.transform.SetParent(slot.transform,false);colliderChild.layer=slot.gameObject.layer;
        var childCollider=colliderChild.AddComponent<BoxCollider>();childCollider.center=rootCollider.center;childCollider.size=rootCollider.size;
        var blocker=GameObject.CreatePrimitive(PrimitiveType.Sphere);blocker.layer=LayerMask.NameToLayer("Enemy");
        var ray=camera.ScreenPointToRay(point);blocker.transform.position=ray.GetPoint(5);
        slot.PlacementPoint.localPosition=new Vector3(.22f,.3f,-.17f);Physics.SyncTransforms();elixir.TrySpend(elixir.Current);
        check(placement.ProcessPointer(point,true) && slot.IsOccupied && slot.Occupant!=null,"3D screen click resolves child slot collider despite nearer enemy/table colliders");
        check(Vector3.Distance(slot.Occupant.transform.position,slot.PlacementPoint.position)<.0001f,"Turret uses configurable PlacementPoint instead of slot root or ray-hit coordinates");
        check(elixir.Current==0 && wallet.Balance==1000-((TowerCardDefinition)selected.Definition).Tower.Cost,"Zero-Elixir Tower places at zero balance while paying existing resource cost");
        check(hand.SelectedCard==null && hand.Cycle.UpcomingQueue.Last()==selected && hand.Cycle.Hand.SequenceEqual(before.Where(c=>c!=selected).Concat(new[]{queue[0]})),"Screen-click success advances the common cycle exactly once");
        slot.Occupant.enabled=false; hand.Select(0); before=hand.Cycle.Hand.ToArray();queue=hand.Cycle.UpcomingQueue.ToArray();selected=hand.SelectedCard;int balance=wallet.Balance;
        check(!placement.ProcessPointer(point,true) && message=="A turret is already placed here." && hand.SelectedCard==selected && wallet.Balance==balance && elixir.Current==0 && hand.Cycle.Hand.SequenceEqual(before) && hand.Cycle.UpcomingQueue.SequenceEqual(queue),"Occupied screen click cannot place twice, spend, clear selection, or rotate cards");
        UnityEngine.Object.DestroyImmediate(blocker);
        TabletopStageValidation.CapturePreview("/tmp/defense-placement-camera-preview.png");
        UnityEngine.Object.DestroyImmediate(colliderChild);rootCollider.enabled=true;
        UnityEngine.Object.DestroyImmediate(slot.Occupant.gameObject);slot.PlacementPoint.localPosition=originalPoint;slot.SetHovered(false);
        hand.Initialize(PlayerSession.Deck,placement,flow);wallet.Initialize(savedGold);elixir.Initialize(flow);Physics.SyncTransforms();
    }
}
}
