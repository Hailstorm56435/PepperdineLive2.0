using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CanonicalNameTableReplacer : EditorWindow
{
    // A simple serializable class to hold the find/replace pair.
    [System.Serializable]
    public class ReplacementPair
    {
        public string find = "";
        public string replace = "";
    }

    // List holding all replacement pairs.
    private List<ReplacementPair> replacementPairs = new List<ReplacementPair>();

    [MenuItem("Tools/Replace Canonical Names Using Table")]
    public static void ShowWindow()
    {
        GetWindow<CanonicalNameTableReplacer>("Canonical Name Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Canonical Name Table Replacer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Display each replacement pair and allow user editing.
        if (replacementPairs != null)
        {
            for (int i = 0; i < replacementPairs.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Text fields for "find" and "replace"
                replacementPairs[i].find = EditorGUILayout.TextField("Find", replacementPairs[i].find);
                replacementPairs[i].replace = EditorGUILayout.TextField("Replace", replacementPairs[i].replace);

                // Remove button for the pair
                if (GUILayout.Button("Remove", GUILayout.MaxWidth(70)))
                {
                    replacementPairs.RemoveAt(i);
                    i--; // adjust the iteration index after removal
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        // Button to add a new replacement pair to the table.
        if (GUILayout.Button("Add Replacement Pair"))
        {
            replacementPairs.Add(new ReplacementPair());
        }

        GUILayout.Space(10);

        // Button to trigger the replacement process for the selected objects.
        if (GUILayout.Button("Apply Replacement on Selected Objects"))
        {
            ReplaceCanonicalNames();
        }
    }

    private void ReplaceCanonicalNames()
    {
        int updatedCount = 0;

        // Iterate over each selected GameObject in the scene.
        foreach (var obj in Selection.gameObjects)
        {
            // Get the BuildingInfo component; skip objects that don't have it.
            BuildingInfo info = obj.GetComponent<BuildingInfo>();
            if (info == null)
                continue;

            SerializedObject so = new SerializedObject(info);
            SerializedProperty canonicalProp = so.FindProperty("canonicalName");
            if (canonicalProp == null)
                continue;

            // Apply replacements to the existing canonical name.
            string originalValue = canonicalProp.stringValue;
            string updatedValue = originalValue;

            foreach (var pair in replacementPairs)
            {
                if (!string.IsNullOrEmpty(pair.find))
                {
                    updatedValue = updatedValue.Replace(pair.find, pair.replace);
                }
            }

            // Only update if a change was detected.
            if (!updatedValue.Equals(originalValue))
            {
                Undo.RecordObject(info, "Replace Canonical Name");
                canonicalProp.stringValue = updatedValue;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(info);
                updatedCount++;
            }
        }

        Debug.Log($"[CanonicalNameTableReplacer] Updated canonicalName for {updatedCount} BuildingInfo objects.");
    }
}
