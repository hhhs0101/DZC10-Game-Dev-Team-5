using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
namespace Defense.Editor {
public static class KitchenBackgroundSetup {
    [MenuItem("Defense/Apply Kitchen Background")]
    public static void Run() {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing the scene.");
        const string imagePath = "Assets/Defense/Art/KitchenBackground.png";
        AssetDatabase.ImportAsset(imagePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(imagePath);
        importer.textureType = TextureImporterType.Default; importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp; importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        var scene = EditorSceneManager.OpenScene("Assets/Defense/Scenes/TestStage.unity");
        var camera = Camera.main;
        const string materialPath = "Assets/Defense/Art/Materials/KitchenBackground.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(Shader.Find("Unlit/Texture")); AssetDatabase.CreateAsset(material,materialPath); }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
        var child = camera.transform.Find("Kitchen Background");
        var root = child != null ? child.gameObject : GameObject.CreatePrimitive(PrimitiveType.Quad);
        root.name = "Kitchen Background"; root.transform.SetParent(camera.transform,false);
        root.layer = LayerMask.NameToLayer("Ignore Raycast");
        var collider = root.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
        var renderer = root.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        var backdrop = root.GetComponent<CameraBackdrop>() ?? root.AddComponent<CameraBackdrop>();
        PrototypeBuilder.Set(backdrop,"worldCamera",camera); backdrop.Refresh();
        EditorUtility.SetDirty(material); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        TabletopStageValidation.CapturePreview("/tmp/kitchen-background-preview.png");
        Debug.Log("KITCHEN_BACKGROUND_APPLIED");
    }
}
}
