using UnityEditor;
using UnityEngine;

public class BuildingInfoNameFixer : EditorWindow
{
    [MenuItem("Tools/Sync Selected BuildingInfo Canonical Names")]
    public static void SyncSelectedCanonicalNames()
    {
        int updatedCount = 0;

        foreach (var obj in Selection.gameObjects)
        {
            BuildingInfo info = obj.GetComponent<BuildingInfo>();
            if (info == null) continue;

            SerializedObject so = new SerializedObject(info);
            SerializedProperty canonicalNameProp = so.FindProperty("canonicalName");

            if (canonicalNameProp != null && canonicalNameProp.stringValue != obj.name)
            {
                Undo.RecordObject(info, "Update Canonical Name");
                canonicalNameProp.stringValue = obj.name;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(info);
                updatedCount++;
            }
        }

        Debug.Log($"[BuildingInfoNameFixer] Updated canonicalName for {updatedCount} selected BuildingInfo objects.");
    }
}
