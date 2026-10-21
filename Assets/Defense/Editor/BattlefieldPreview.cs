using UnityEngine;
namespace Defense.Editor {
public static class BattlefieldPreview {
    public static void Capture(string outputPath = "/tmp/defense-stage1-preview.png") {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        var camera=Camera.main; var previous=camera.targetTexture; var active=RenderTexture.active;
        var target=new RenderTexture(1280,720,24); var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try {
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
            System.IO.File.WriteAllBytes(outputPath,texture.EncodeToPNG());
        } finally {
            camera.targetTexture=previous; RenderTexture.active=active; target.Release();
            UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
}
