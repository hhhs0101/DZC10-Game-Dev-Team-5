using UnityEditor;
using UnityEngine;
namespace Defense.Editor {
// Shared serialized-reference helper used by the retained 2D authoring tools.
public static class PrototypeBuilder {
    public static void Set(Object target, string field, Object value) {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
}
