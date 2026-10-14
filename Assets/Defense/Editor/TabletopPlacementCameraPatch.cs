using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Defense.Editor {
// Explicit authoring command, not a runtime camera controller.
public static class TabletopPlacementCameraPatch {
    [MenuItem("Defense/Apply 3D Placement and Camera Patch")]
    public static void Run() {
        if(EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before authoring the Scene.");
        var scene=EditorSceneManager.OpenScene("Assets/Defense/Scenes/TestStage.unity");
        var camera=Camera.main;
        camera.transform.SetPositionAndRotation(new Vector3(0,18*Mathf.Tan(40*Mathf.Deg2Rad),-18),Quaternion.Euler(40,0,0));
        camera.orthographic=false; camera.fieldOfView=45;
        foreach(var slot in Object.FindObjectsByType<TowerPlacementSlot>(FindObjectsSortMode.None)) {
            if(slot.PlacementPoint != slot.transform) continue;
            var point=new GameObject("PlacementPoint").transform; point.SetParent(slot.transform,false);
            point.localPosition=new Vector3(0,.07f,0);
            PrototypeBuilder.Set(slot,"placementPoint",point);
        }
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("PLACEMENT_CAMERA_PATCH_APPLIED");
    }
}
}
