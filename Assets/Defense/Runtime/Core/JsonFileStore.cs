using System;
using System.IO;
using UnityEngine;
namespace Defense {
// Shared JSON storage; callers own validation and domain state.
public sealed class JsonFileStore<T> where T : class {
    public string SavePath { get; }
    private readonly string label;
    private readonly Action<string> warning;
    private readonly Action<string> error;
    public JsonFileStore(string path,string description,Action<string> logWarning = null,Action<string> logError = null) {
        SavePath = Path.GetFullPath(path); label = description;
        warning = logWarning ?? (message=>Debug.LogWarning(message));
        error = logError ?? (message=>Debug.LogError(message));
    }
    public T Load() {
        try {
            if (!File.Exists(SavePath)) return null;
            var data = JsonUtility.FromJson<T>(File.ReadAllText(SavePath));
            if (data == null) warning(label+" save is empty; using default data: "+SavePath);
            return data;
        } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException) {
            warning("Could not load "+label+" save; using default data. "+SavePath+": "+exception.Message);
            return null;
        }
    }
    public bool Save(T data) {
        string temporary = SavePath+".tmp";
        try {
            string json = JsonUtility.ToJson(data,true);
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            using (var stream = new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)) {
                var bytes = new System.Text.UTF8Encoding(false).GetBytes(json);
                stream.Write(bytes,0,bytes.Length); stream.Flush(true);
            }
            // Keep the previous save intact if writing or replacement fails.
            if (File.Exists(SavePath)) File.Replace(temporary,SavePath,null);
            else File.Move(temporary,SavePath);
            return true;
        } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is NotSupportedException) {
            error(label+" save failed; changes remain in memory but were NOT saved. "+SavePath+": "+exception.Message);
            return false;
        } finally {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) {
                warning("Could not remove temporary "+label+" save: "+exception.Message);
            }
        }
    }
}
}
