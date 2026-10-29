using System.IO;
using UnityEngine;
namespace Defense {
// Shared project/installation-local directory for player history JSON files.
public static class PlayerSavePaths {
    public static string FilePath(string fileName) {
        // Editor: project folder. Desktop player: beside the executable / macOS app.
        string root=Path.GetFullPath(Path.Combine(Application.dataPath,
            Application.platform==RuntimePlatform.OSXPlayer ? "../.." : ".."));
        return Path.Combine(root,"SaveData",fileName);
    }
}
}
